#!/usr/bin/env python3
"""Explicit network smoke test; no job qualification or record writes."""
import json
import sys
from verify_ats import retrieve_board, identify

def main():
    results=[]
    for provider,board in [('ashby','ashby'),('greenhouse','launchdarkly'),('lever','standtogether')]:
        r=retrieve_board(provider,board)
        jobs=r.pop('jobs')
        r['posting_count']=len(jobs)
        r['pass']=r['status']=='published' and bool(jobs) and all(j['posting_id'] and j['title'] and j['job_url'] and j['apply_url'] for j in jobs)
        if provider!='greenhouse':
            r['pass']=r['pass'] and all(identify(j['job_url'])==(provider,board,j['posting_id']) for j in jobs)
        results.append(r)
    print(json.dumps(results,indent=2))
    return 0 if all(r['pass'] for r in results) else 1
if __name__=='__main__': sys.exit(main())
