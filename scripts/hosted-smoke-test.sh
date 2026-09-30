#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 https://service.onrender.com [--expect-registration-closed]" >&2
}

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
  usage
  exit 2
fi

base_url="${1%/}"
expect_registration_closed=false
if [ "${2:-}" = "--expect-registration-closed" ]; then
  expect_registration_closed=true
elif [ "$#" -eq 2 ]; then
  usage
  exit 2
fi

if [[ "$base_url" != https://* ]] && [ "${ALLOW_HTTP:-false}" != "true" ]; then
  echo "Hosted smoke tests require an HTTPS URL." >&2
  exit 2
fi

work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

request() {
  local name="$1"
  local path="$2"
  local expected_status="$3"
  shift 3

  local headers="$work_dir/$name.headers"
  local body="$work_dir/$name.body"
  local status
  status="$(curl --silent --show-error \
    --connect-timeout 15 \
    --max-time 90 \
    --retry 5 \
    --retry-delay 5 \
    --retry-all-errors \
    --dump-header "$headers" \
    --output "$body" \
    --write-out '%{http_code}' \
    "$@" \
    "$base_url$path")"

  if [ "$status" != "$expected_status" ]; then
    echo "$name returned HTTP $status; expected $expected_status." >&2
    cat "$body" >&2
    exit 1
  fi

  if [[ "$path" == /api/* ]] && ! grep -Eiq '^X-Correlation-ID: .+' "$headers"; then
    echo "$name did not return an X-Correlation-ID header." >&2
    exit 1
  fi
}

request live /api/health/live 200
grep -q '"status":"ok"' "$work_dir/live.body"

request ready /api/health/ready 200
grep -q '"status":"ready"' "$work_dir/ready.body"

request shell / 200
grep -q '<app-root>' "$work_dir/shell.body"

request manifest /manifest.webmanifest 200
grep -q '"display": "standalone"' "$work_dir/manifest.body"

request unauthenticated /api/account/me 401
request unknown-api /api/hosted-smoke-test-not-found 404

if [ "$expect_registration_closed" = true ]; then
  request registration /api/account/register 404 \
    --request POST \
    --header 'Content-Type: application/json' \
    --header 'X-Agentic-Request: 1' \
    --data '{"displayName":"Smoke Test","email":"smoke-test@example.invalid","password":"not-a-real-password","bootstrapToken":"invalid"}'
fi

echo "Hosted smoke checks passed for $base_url"
