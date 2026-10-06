#!/usr/bin/env python3
"""Private PostgreSQL custom-format backups and restore verification."""
import argparse
import datetime as dt
import getpass
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import uuid


def run(tool, *args):
    if not shutil.which(tool):
        raise ValueError(f'Install PostgreSQL client tools: {tool} is missing.')
    result = subprocess.run([tool, *args], capture_output=True, text=True)
    if result.returncode:
        # PostgreSQL errors can include connection credentials or row contents.
        raise ValueError(f'{tool} failed (exit {result.returncode}); check connectivity, client version and permissions. Details suppressed to protect private data.')
    return result.stdout


def sql(query):
    return run('psql', '-X', '--no-password', '-A', '-t', '-v', 'ON_ERROR_STOP=1', '-c', query).strip()


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def tables():
    return json.loads(sql("SELECT coalesce(json_agg(tablename ORDER BY tablename), '[]'::json) FROM pg_tables WHERE schemaname='public'"))


def quote(name):
    return '"' + name.replace('"', '""') + '"'


def verify():
    names = tables()
    required = {'Jobs', 'UserAccounts', 'CandidateProfiles', 'Applications', 'DataProtectionKeys', '__EFMigrationsHistory'}
    if not required.issubset(names):
        raise ValueError('Restored database is missing required application tables.')
    counts = {name: int(sql(f'SELECT count(*) FROM public.{quote(name)}')) for name in names}
    migrations = json.loads(sql('SELECT coalesce(json_agg("MigrationId" ORDER BY "MigrationId"), \'[]\'::json) FROM "__EFMigrationsHistory"'))
    if not migrations:
        raise ValueError('Migration history is empty.')
    if sql("SELECT count(*) FROM pg_constraint c JOIN pg_namespace n ON n.oid=c.connamespace WHERE n.nspname='public' AND c.contype IN ('f','c') AND NOT c.convalidated") != '0':
        raise ValueError('Database contains unvalidated constraints.')
    return {'checked_at_utc': dt.datetime.now(dt.timezone.utc).isoformat(), 'migrations': migrations, 'row_counts': counts}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    backup = sub.add_parser('backup')
    backup.add_argument('--directory', required=True, type=Path)
    restore = sub.add_parser('restore')
    restore.add_argument('archive', type=Path)
    restore.add_argument('--confirm-target', required=True)
    sub.add_parser('verify')
    args = parser.parse_args()
    os.umask(0o077)
    for key in ('PGHOST', 'PGDATABASE', 'PGUSER'):
        if not os.environ.get(key):
            raise ValueError(f'{key} must be set explicitly.')
    if not (os.environ.get('PGPASSFILE') or os.environ.get('PGPASSWORD')):
        os.environ['PGPASSWORD'] = getpass.getpass('Database password: ')
    if args.command == 'backup':
        directory = args.directory.expanduser().resolve()
        repo = Path(__file__).resolve().parent.parent
        if directory == repo or repo in directory.parents:
            raise ValueError('Choose a backup directory outside the repository.')
        directory.mkdir(parents=True, exist_ok=True)
        stamp = dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
        archive = directory / f'agentic-job-search-{stamp}-{uuid.uuid4().hex[:8]}.dump'
        partial = archive.with_suffix('.partial')
        try:
            run('pg_dump', '--no-password', '--format=custom', '--no-owner', '--no-acl', '--file', str(partial))
            run('pg_restore', '--list', str(partial))
            partial.rename(archive)
            archive.with_suffix('.sha256').write_text(digest(archive) + '\n')
        finally:
            partial.unlink(missing_ok=True)
        print(f'Backup saved: {archive}')
    elif args.command == 'restore':
        archive = args.archive.expanduser().resolve()
        if args.confirm_target != os.environ['PGDATABASE']:
            raise ValueError('--confirm-target must exactly match PGDATABASE.')
        if archive.with_suffix('.sha256').read_text().strip() != digest(archive):
            raise ValueError('Backup checksum mismatch; restore refused.')
        if sql("SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND n.nspname NOT LIKE 'pg_toast%' AND n.nspname NOT LIKE 'pg_temp%' AND c.relkind IN ('r','p','v','m','S','f')") != '0':
            raise ValueError('Target database is not empty; restore refused.')
        run('pg_restore', '--no-password', '--exit-on-error', '--single-transaction', '--no-owner', '--no-acl', '--dbname', os.environ['PGDATABASE'], str(archive))
        report = verify()
        report['archive_sha256'] = digest(archive)
        print(json.dumps(report, indent=2))
    else:
        print(json.dumps(verify(), indent=2))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, KeyboardInterrupt):
        # Never include an exception whose message might contain secrets.
        if isinstance(sys.exception(), ValueError):
            print(str(sys.exception()), file=sys.stderr)
        else:
            print('Operation stopped; check input files and permissions.', file=sys.stderr)
        sys.exit(1)
