#!/usr/bin/env python3
"""Exercise real backup/restore against isolated PostgreSQL; no hosted secrets."""
import json
import os
import re
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import uuid

ROOT = Path(__file__).resolve().parent.parent
NAME = 'agentic-recovery-' + uuid.uuid4().hex[:12]


def command(args, env=None, expected=0, error=None):
    result = subprocess.run(args, cwd=ROOT, env=env, capture_output=True, text=True)
    if result.returncode != expected:
        raise RuntimeError(f'Command failed: {args[0]}\n{result.stdout}\n{result.stderr}')
    if error and error not in result.stderr:
        raise RuntimeError(f'Expected refusal missing: {error}')
    return result.stdout.strip()


try:
    command(['docker', 'run', '-d', '--name', NAME, '-e', 'POSTGRES_PASSWORD=fixture-only', '-p', '127.0.0.1::5432', os.environ.get('RECOVERY_POSTGRES_IMAGE', 'postgres:' + re.search(r'\d+', command(['pg_dump', '--version'])).group() + '-alpine')])
    port = command(['docker', 'port', NAME, '5432/tcp']).split(':')[-1]
    env = dict(os.environ, PGHOST='127.0.0.1', PGPORT=port, PGUSER='postgres', PGDATABASE='postgres', PGPASSWORD='fixture-only', PGSSLMODE='disable')
    for _ in range(60):
        ready = subprocess.run(['pg_isready'], env=env, capture_output=True)
        if ready.returncode == 0:
            break
        time.sleep(1)
    else:
        raise RuntimeError('Disposable PostgreSQL did not become ready.')

    def query(sql):
        return command(['psql', '-X', '-At', '-v', 'ON_ERROR_STOP=1', '-c', sql], env)

    query('CREATE DATABASE recovery_source')
    query('CREATE DATABASE recovery_target')
    env['PGDATABASE'] = 'recovery_source'
    migrate_env = dict(env, ASPNETCORE_ENVIRONMENT='Testing', ConnectionStrings__JobSearch=f'Host=127.0.0.1;Port={port};Database=recovery_source;Username=postgres;Password=fixture-only')
    command(['dotnet', 'run', '--project', 'src/AgenticJobSearch.Api', '--configuration', 'Release', '--no-launch-profile', '--', '--migrate'], migrate_env)
    # Populate real migrated tables with deterministic fictional records, including FK links.
    ids = {t: str(uuid.uuid4()) for t in ['UserAccounts', 'CandidateProfiles', 'CandidateEvidence', 'Jobs', 'Applications']}
    links = {'OwnerId': ids['UserAccounts'], 'CandidateProfileId': ids['CandidateProfiles'], 'JobId': ids['Jobs']}
    for table, identity in ids.items():
        columns = json.loads(query(f"SELECT json_agg(json_build_object('name',column_name,'type',data_type)) FROM information_schema.columns WHERE table_schema='public' AND table_name='{table}' AND is_nullable='NO' AND column_default IS NULL"))
        names, values = [], []
        for col in columns:
            names.append('"' + col['name'] + '"')
            kind = col['type']
            if kind == 'uuid':
                value = "'" + (identity if col['name'] == 'Id' else links.get(col['name'], str(uuid.uuid4()))) + "'"
            elif kind in ('integer', 'bigint', 'numeric', 'double precision'):
                value = '1'
            elif kind == 'boolean':
                value = 'false'
            elif 'timestamp' in kind:
                value = "'2026-01-01T00:00:00Z'"
            else:
                value = "'fictional recovery fixture'"
            values.append(value)
        query(f'INSERT INTO "{table}" ({",".join(names)}) VALUES ({",".join(values)})')
    query('INSERT INTO "DataProtectionKeys" ("FriendlyName", "Xml") VALUES (\'fixture\', \'<fixture/>\')')
    def snapshot():
        return {t: query(f'SELECT row_to_json(t) FROM "{t}" t ORDER BY "Id"') for t in [*ids, 'DataProtectionKeys']}
    before = snapshot()
    tool = [sys.executable, 'scripts/database-backup.py']
    with tempfile.TemporaryDirectory() as directory:
        command(tool + ['backup', '--directory', directory], env)
        archive = next(Path(directory).glob('*.dump'))
        assert archive.stat().st_mode & 0o077 == 0
        env['PGDATABASE'] = 'recovery_target'
        command(tool + ['restore', str(archive), '--confirm-target', 'wrong'], env, expected=1, error='must exactly match')
        assert query("SELECT count(*) FROM pg_tables WHERE schemaname='public'") == '0'
        report = json.loads(command(tool + ['restore', str(archive), '--confirm-target', 'recovery_target'], env))
        assert report['row_counts']['Jobs'] == 1
        assert snapshot() == before
        command(tool + ['restore', str(archive), '--confirm-target', 'recovery_target'], env, expected=1, error='Target database is not empty')
        assert snapshot() == before
        with archive.open('ab') as stream:
            stream.write(b'damaged')
        command(tool + ['restore', str(archive), '--confirm-target', 'recovery_target'], env, expected=1, error='checksum mismatch')
        print('Recovery passed: migrated schema, related records, session keys, target guard, populated-target refusal and checksum corruption refusal.')
finally:
    subprocess.run(['docker', 'rm', '-f', NAME], capture_output=True)
