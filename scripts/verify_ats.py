#!/usr/bin/env python3
"""Read-only public ATS posting verification. No applications are submitted.

Usage: python3 scripts/verify_ats.py https://jobs.ashbyhq.com/COMPANY/POSTING_ID
       python3 scripts/verify_ats.py URL --json
Exit: 0 published; 2 not found; 3 inconclusive/unsupported.
"""
import argparse
import json
import re
import sys
from urllib.parse import urlparse, parse_qs, quote
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError


def identify(url):
    parsed = urlparse(url)
    host = (parsed.hostname or "").lower()
    parts = [p for p in parsed.path.split("/") if p]
    if host == "jobs.ashbyhq.com" and len(parts) >= 2:
        return ("ashby", parts[0], parts[1])
    if host in ("job-boards.greenhouse.io", "boards.greenhouse.io") and len(parts) >= 2 and parts[1] == "jobs":
        job_id = parts[2] if len(parts) >= 3 else parse_qs(parsed.query).get("gh_jid", [""])[0]
        if job_id:
            return ("greenhouse", parts[0], job_id)
    if host == "jobs.lever.co" and len(parts) >= 2:
        return ("lever", parts[0], parts[1])
    return None


def get_json(url, timeout=12):
    req = Request(url, headers={"User-Agent": "JobSearchVerifier/0.1 (public job boards)", "Accept": "application/json"})
    with urlopen(req, timeout=timeout) as r:
        return json.load(r)


def api_endpoint(provider, board):
    board = quote(board, safe="")
    if provider == "ashby":
        return "https://api.ashbyhq.com/posting-api/job-board/" + board + "?includeCompensation=true"
    if provider == "greenhouse":
        return "https://boards-api.greenhouse.io/v1/boards/" + board + "/jobs?content=true"
    return "https://api.lever.co/v0/postings/" + board + "?mode=json"


def match_job(provider, jobs, job_id):
    target = job_id.lower()
    for job in jobs:
        if provider == "ashby":
            candidates = [str(job.get("id", "")), str(job.get("jobUrl", "")), str(job.get("applyUrl", ""))]
            if any(re.search(r"(?:/|^)" + re.escape(target) + r"(?:/|\?|$)", c.lower()) for c in candidates):
                return job
        elif provider == "greenhouse" and str(job.get("id", "")).lower() == target:
            return job
        elif provider == "lever" and str(job.get("id", "")).lower() == target:
            return job
    return None


def verify(url, fetch=get_json):
    identity = identify(url)
    base = dict(input_url=url, status="unverified", provider=None, board=None, posting_id=None, title=None, location=None, salary_text=None, job_url=None, apply_url=None, reason=None)
    if not identity:
        return dict(base, reason="unsupported URL or job ID unavailable")
    provider, board, job_id = identity
    base.update(provider=provider, board=board, posting_id=job_id)
    try:
        data = fetch(api_endpoint(provider, board))
    except (HTTPError, URLError, TimeoutError, ValueError, OSError) as exc:
        return dict(base, reason="ATS API request failed: " + str(exc))
    jobs = data.get("jobs", []) if provider in ("ashby", "greenhouse") and isinstance(data, dict) else data
    if not isinstance(jobs, list):
        return dict(base, reason="unexpected ATS API response")
    job = match_job(provider, jobs, job_id)
    if job is None:
        return dict(base, status="not_found", reason="posting ID absent from published board; may be unpublished, unlisted, or changed")
    if provider == "ashby":
        compensation = job.get("compensation") or {}
        salary = compensation.get("scrapeableCompensationSalarySummary") or compensation.get("compensationTierSummary")
        return dict(base, status="published", title=job.get("title"), location=job.get("location"), salary_text=salary, job_url=job.get("jobUrl"), apply_url=job.get("applyUrl"), reason="present on public ATS board; does not guarantee application submission succeeds")
    if provider == "greenhouse":
        return dict(base, status="published", title=job.get("title"), location=(job.get("location") or {}).get("name"), salary_text=None, job_url=job.get("absolute_url"), apply_url=job.get("absolute_url"), reason="present on public ATS board; compensation may require separate review")
    return dict(base, status="published", title=job.get("text"), location=(job.get("categories") or {}).get("location"), salary_text=None, job_url=job.get("hostedUrl"), apply_url=job.get("applyUrl"), reason="present on public ATS board; compensation may require separate review")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("url")
    p.add_argument("--json", action="store_true", help="machine-readable output")
    args = p.parse_args()
    result = verify(args.url)
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return {"published": 0, "not_found": 2, "unverified": 3}[result["status"]]


if __name__ == "__main__":
    sys.exit(main())
