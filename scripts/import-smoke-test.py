#!/usr/bin/env python3
"""Exercise the disposable import image through HTTP, using fictional records only."""
import argparse
import json
import secrets
import subprocess
import time
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--image', default='agentic-job-import-test:local')
args = parser.parse_args()
suffix = secrets.token_hex(5)
network, database, api = (f'import-test-{suffix}-{name}' for name in ('network', 'db', 'api'))
token = secrets.token_hex(32)
sha = '1' * 40

def docker(*arguments):
    return subprocess.check_output(['docker', *arguments], text=True, stderr=subprocess.STDOUT).strip()

def wait_for(check):
    deadline = time.monotonic() + 60
    while time.monotonic() < deadline:
        try:
            return check()
        except (OSError, subprocess.CalledProcessError):
            time.sleep(0.5)
    raise RuntimeError('Disposable service did not become ready within 60 seconds')

try:
    docker('network', 'create', network)
    docker('run', '-d', '--name', database, '--network', network,
           '-e', 'POSTGRES_PASSWORD=fictional-test-password', '-e', 'POSTGRES_DB=import_test', 'postgres:16-alpine')
    wait_for(lambda: docker('exec', database, 'pg_isready', '-h', '127.0.0.1', '-U', 'postgres'))
    docker('run', '-d', '--platform', 'linux/amd64', '--name', api, '--network', network,
           '-p', '127.0.0.1::8080', '-e', f'Imports__Token={token}', '-e', f'TestSource__Head={sha}',
           '-e', f'ConnectionStrings__JobSearch=Host={database};Database=import_test;Username=postgres;Password=fictional-test-password', args.image)
    port = docker('port', api, '8080/tcp').split(':')[-1]
    origin = f'http://127.0.0.1:{port}'
    def call(path, payload=None):
        request = urllib.request.Request(origin + path,
            data=None if payload is None else json.dumps(payload).encode(),
            headers={'Authorization': f'Bearer {token}', 'Content-Type': 'application/json'})
        with urllib.request.urlopen(request, timeout=5) as response:
            return json.load(response)
    wait_for(lambda: call('/api/health'))
    path = '/api/v1/imports/job-records'
    payload = {'schema_version': '1.0', 'dry_run': True,
        'source': {'repository': 'RRKaze/job-search', 'branch': 'main', 'commit_sha': sha, 'expected_previous_commit': None},
        'jobs': [{'job_id': 'JOB-FICTIONAL', 'company': 'Fictional Example', 'role': 'Engineer', 'location': 'Remote',
                  'salary_text': None, 'priority': 'high', 'fit_rationale': None, 'gaps_notes': None, 'stage': 'lead',
                  'status_date': None, 'source_url': 'https://example.com/jobs/1', 'verified_date': '2026-09-21'}],
        'applications': []}
    assert call(path, payload)['result'] == 'validated'
    assert call(path + '/checkpoint')['source_commit'] is None
    payload['dry_run'] = False
    assert call(path, payload)['result'] == 'imported'
    assert call(path, payload)['result'] == 'already_imported'
    assert call(path + '/checkpoint')['source_commit'] == sha
    # Imports use a separate machine token and must not grant browser-account access.
    count = docker('exec', database, 'psql', '-U', 'postgres', '-d', 'import_test', '-tAc', 'SELECT count(*) FROM "Jobs"')
    assert count == '1'
    print('Docker HTTP smoke test passed: dry-run, apply, retry, checkpoint, single job.')
finally:
    for name in (api, database):
        subprocess.run(['docker', 'rm', '-fv', name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['docker', 'network', 'rm', network], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
