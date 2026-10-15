#!/usr/bin/env python3
"""Retrieve public ATS boards into a review queue; never write canonical records.

Input JSON: [{provider, board, company, bucket, family, query}]. Outputs include
full posting evidence, prior-seen ledger, request timings and a review queue.
Default all new candidates to Hold. An explicit reviewed decision file can be
compiled to the canonical audit schema with --reviews. No automatic Apply score.
"""
import argparse
import csv
from datetime import datetime
import hashlib
import json
from pathlib import Path
import re
import time
from urllib.parse import urlsplit, urlunsplit, parse_qsl, urlencode
from zoneinfo import ZoneInfo
from verify_ats import identify, retrieve_board

AUDIT_FIELDS='run_id reviewed_at company role source_url discovery_source location salary_text decision primary_reason notes lead_bucket'.split()


def normalized_url(url):
    identity=identify(url)
    if identity:
        return ':'.join(identity)
    p=urlsplit(url.strip())
    query=urlencode(sorted(set((k,v) for k,v in parse_qsl(p.query) if not k.lower().startswith('utm_') and k.lower() not in ('source','gh_src','lever-source','ref','referrer'))))
    return urlunsplit((p.scheme.lower(),p.netloc.lower(),p.path.rstrip('/'),query,''))


def words(text):
    return ' '.join(re.findall(r'[a-z0-9]+', (text or '').lower()))


def role_key(row,location=False):
    company=words(row.get('company',''))
    company=re.sub(r'\b(inc|llc|ltd)\b','',company).strip()
    return (company,words(row.get('role') or row.get('title',''))) + ((words(row.get('location','')),) if location else ())


def load_seen(canonical):
    canonical=Path(canonical)
    tracked_urls=set(); tracked_roles=set(); seen={}; seen_roles={}
    paths=[canonical/'data/jobs.csv']+sorted((canonical/'data/search-audits').glob('*.csv'))
    if not paths[0].is_file():
        raise ValueError('canonical data/jobs.csv required')
    for path in paths:
        with path.open(newline='',encoding='utf-8-sig') as f:
            for row in csv.DictReader(f):
                url=normalized_url(row.get('source_url',''))
                if not url: continue
                if path==paths[0]:
                    tracked_urls.add(url); tracked_roles.add(role_key(row))
                else:
                    stamp=row.get('reviewed_at') or row.get('run_id','')
                    item=seen.setdefault(url,dict(first_seen=stamp,last_checked=stamp,decision=row.get('decision'),source=str(path.name)))
                    if stamp<item['first_seen']: item['first_seen']=stamp
                    if stamp>=item['last_checked']: item.update(last_checked=stamp,decision=row.get('decision'),source=path.name)
                    seen_roles[role_key(row,True)]=item
    return tracked_urls,tracked_roles,seen,seen_roles


def classify_seen(job,company,index):
    urls,roles,seen,seen_roles=index
    key=normalized_url(job['job_url'] or '')
    row=dict(company=company,role=job['title'],location=job['location'])
    if key in urls or role_key(row) in roles: return 'already_tracked',None
    prior=seen.get(key) or seen_roles.get(role_key(row,True))
    return ('previously_seen' if prior else 'new'),prior


def run(manifest,canonical,output,title_pattern):
    output=Path(output); output.mkdir(parents=True,exist_ok=True)
    index=load_seen(canonical)
    start=time.monotonic(); now=datetime.now(ZoneInfo('America/New_York'))
    run_id=now.strftime('%Y-%m-%d-%H%M')+'-v2-api'
    cache={}; candidates={}; coverage=[]
    for source in manifest:
        key=(source['provider'],source['board'].lower())
        cached=key in cache
        if not cached: cache[key]=retrieve_board(*key)
        board=cache[key]
        matched=0
        posting_url=source.get('posting_url')
        requested=identify(posting_url) if posting_url else None
        if posting_url and (not requested or requested[:2]!=key):
            raise ValueError('posting_url must match manifest provider and board')
        board_jobs=board['jobs']
        if requested:
            board_jobs=[j for j in board_jobs if j['posting_id']==requested[2]]
            if not board_jobs:
                status='not_found' if board['status']=='published' else 'unverified'
                stub=dict(provider=key[0],board=key[1],posting_id=requested[2],job_url=posting_url,apply_url=None,
                          title=source.get('role','Unknown posting'),location='',salary_text=None,description='',is_listed=False)
                seen,prior=classify_seen(stub,source['company'],index)
                candidates[normalized_url(posting_url)]=dict(stub,company=source['company'],seen=seen,prior=prior,
                    status=status,checked_at=board['checked_at'],evidence_sha256=None,decision='Hold',
                    primary_reason='expired_inaccessible',discovery_source=source['query'])
        for j in board_jobs:
            if not requested and not re.search(title_pattern,j['title'],re.I): continue
            if not j['job_url']: continue
            matched+=1
            url_key=normalized_url(j['job_url'])
            if url_key in candidates: continue
            seen,prior=classify_seen(j,source['company'],index)
            fingerprint=hashlib.sha256(json.dumps(j,sort_keys=True).encode()).hexdigest()
            candidates[url_key]=dict(j,company=source['company'],seen=seen,prior=prior,
                evidence_sha256=fingerprint,checked_at=board['checked_at'],status='published',
                discovery_source=f"{source['family']}: {source['query']}",
                decision='Skip' if seen=='already_tracked' else 'Hold',
                primary_reason='already_tracked' if seen=='already_tracked' else 'other')
        coverage.append(dict(source,cached=cached,status=board['status'],reason=board['reason'],
                             elapsed_seconds=0 if cached else board['elapsed_seconds'],
                             returned=len(board['jobs']),title_matched=matched,endpoint=board['endpoint']))
    result=dict(run_id=run_id,reviewed_at=now.isoformat(),elapsed_seconds=round(time.monotonic()-start,3),
                coverage=coverage,candidates=list(candidates.values()))
    (output/'retrieval.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n')
    (output/'seen-ledger.json').write_text(json.dumps(index[2],indent=2)+'\n')
    (output/'boards.json').write_text(json.dumps(list(cache.values()),ensure_ascii=False)+'\n')
    return result


def compile_audit(retrieval,reviews,policy=None):
    policy=policy or {}
    threshold=policy.get("compensation_policy",{}).get("minimum_acceptable_base_salary_usd",150000)
    by_url={normalized_url(r['source_url']):r for r in reviews}
    if len(by_url)!=len(reviews): raise ValueError('duplicate reviewed URLs')
    candidates={normalized_url(j['job_url']):j for j in retrieval['candidates']}
    rows=[]
    apply_roles=set()
    for key,review in by_url.items():
        if key not in candidates: raise ValueError('review URL not in retrieved evidence')
        job=candidates[key]
        decision=review['decision']; reason=review['primary_reason']; notes=review['notes']
        if decision not in ('Apply','Hold','Skip'): raise ValueError('invalid decision')
        if job['seen']=='already_tracked': decision,reason='Skip','already_tracked'
        bucket=review.get('lead_bucket','other')
        if bucket not in ('remote','relocation_hybrid','other'): raise ValueError('invalid bucket')
        if decision=='Apply' and role_key(job) in apply_roles:
            decision,reason='Skip','already_tracked'
            notes+=' | Duplicate company/role within this run'
        if decision=='Apply':
            if review.get('evidence_sha256')!=job['evidence_sha256']:
                raise ValueError('Apply review must bind to retrieved evidence hash')
            if review.get('location_country')!='US':
                raise ValueError('Apply requires explicit US location evidence')
            if review.get('base_salary_max_usd',0)<=threshold or not review.get('base_salary_evidence'):
                raise ValueError('Apply needs annual USD base above threshold, not total compensation')
            if review.get('base_salary_min_usd',0)<=threshold and not review.get('above_threshold_rationale'):
                raise ValueError('Crossing salary range requires realistic above-threshold rationale')
            if bucket=='relocation_hybrid':
                state=review.get('office_state','').lower()
                city=review.get('office_city','').lower()
                excluded={x.lower() for x in policy.get('excluded_relocation_destinations',['California','Massachusetts','Chicago, Illinois'])}
                if not state or not city or not review.get('relocation_required'):
                    raise ValueError('hybrid requires explicit office and relocation evidence')
                if state in ('ca','ma') or state in excluded or city=='chicago' or f'{city}, {state}' in excluded:
                    raise ValueError('excluded relocation destination')
            checks=review.get('checks',{})
            required=('us_location','base_salary','technical_fit','seniority','eligibility','work_arrangement')
            if not all(checks.get(k) is True for k in required): raise ValueError('Apply requires all explicit qualification checks')
            if bucket=='other' or job['status']!='published' or not job['apply_url'] or not job['is_listed']:
                raise ValueError('Apply requires listed, published, actionable eligible bucket')
            if not (review.get('salary_text') or job['salary_text']): raise ValueError('Apply needs compensation evidence')
            if job['seen']=='previously_seen' and not review.get('recheck_reason'):
                raise ValueError('prior posting requires documented recheck reason')
            if bucket=='relocation_hybrid' and not review.get('onsite_cadence'):
                raise ValueError('hybrid Apply requires onsite cadence')
        if decision=='Apply': apply_roles.add(role_key(job))
        notes+=' | ATS: '+job['status']+' '+job['checked_at']+'; '+job['seen']+'; evidence SHA256 '+str(job['evidence_sha256'])
        if review.get('recheck_reason'): notes+=' | Recheck: '+review['recheck_reason']
        rows.append(dict(run_id=retrieval['run_id'],reviewed_at=review.get('reviewed_at') or datetime.now(ZoneInfo('America/New_York')).isoformat(),company=job['company'],role=job['title'],source_url=job['job_url'],discovery_source=job['discovery_source'],location=review.get('location') or job['location'] or '',salary_text=review.get('salary_text') or job['salary_text'] or '',decision=decision,primary_reason=reason,notes=notes,lead_bucket=bucket))
    return rows


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--manifest',type=Path)
    p.add_argument('--canonical',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    p.add_argument('--title-pattern',default=r'(senior|sr\.?|engineer iii|backend|platform|identity|authentication)')
    p.add_argument('--reviews',type=Path)
    args=p.parse_args()
    if args.reviews:
        result=json.loads((args.output/'retrieval.json').read_text())
        # Recheck canonical dedup at audit time, even if it changed since retrieval.
        index=load_seen(args.canonical)
        for j in result['candidates']: j['seen'],j['prior']=classify_seen(j,j['company'],index)
        policy=json.loads((args.canonical/'data/search-profile.json').read_text())
        rows=compile_audit(result,json.loads(args.reviews.read_text()),policy)
        with (args.output/'audit.csv').open('w',newline='') as f:
            w=csv.DictWriter(f,fieldnames=AUDIT_FIELDS); w.writeheader(); w.writerows(rows)
        print(json.dumps({'assessed':len(rows),'decisions':{d:sum(r['decision']==d for r in rows) for d in ('Apply','Hold','Skip')},'apply_buckets':{b:sum(r['decision']=='Apply' and r['lead_bucket']==b for r in rows) for b in ('remote','relocation_hybrid')}}))
    else:
        if not args.manifest: p.error('--manifest required for retrieval')
        result=run(json.loads(args.manifest.read_text()),args.canonical,args.output,args.title_pattern)
        print(json.dumps({k:v for k,v in result.items() if k!='candidates'}))
        print('Review queue:',len(result['candidates']))

if __name__=='__main__': main()
