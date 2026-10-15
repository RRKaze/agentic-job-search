#!/usr/bin/env python3
"""Read-only public ATS verification. Exit 0 published, 2 absent, 3 unverified.

A published result is evidence of a listing, never an application submission or
qualification decision. No browser, credentials, forms or arbitrary URL fetching.
"""
import argparse
from datetime import datetime, timezone
from html import unescape
import json
import re
import sys
import time
from urllib.parse import urlparse, parse_qs, quote
from urllib.request import Request, build_opener, HTTPRedirectHandler
from urllib.error import HTTPError, URLError

HOSTS = {'jobs.ashbyhq.com':'ashby', 'jobs.lever.co':'lever',
         'jobs.eu.lever.co':'lever_eu', 'boards.greenhouse.io':'greenhouse',
         'job-boards.greenhouse.io':'greenhouse'}
API_HOSTS = {'api.ashbyhq.com', 'boards-api.greenhouse.io', 'api.lever.co', 'api.eu.lever.co'}
TOKEN = re.compile(r'^[A-Za-z0-9_-]+$')


def identify(url):
    try:
        p = urlparse(url)
        if p.scheme != 'https' or p.username or p.password or p.port not in (None,443):
            return None
        provider = HOSTS.get((p.hostname or '').lower())
        parts = [s for s in p.path.split('/') if s]
        if not provider or not parts or not all(TOKEN.fullmatch(s) for s in parts):
            return None
        if provider == 'greenhouse':
            if len(parts)==3 and parts[1]=='jobs':
                job_id=parts[2]
            elif len(parts)==2 and parts[1]=='jobs':
                job_id=parse_qs(p.query).get('gh_jid',[''])[0]
            else:
                return None
        elif len(parts) in (2,3) and (len(parts)==2 or parts[2] in ('apply','application')):
            job_id=parts[1]
        else:
            return None
        if not TOKEN.fullmatch(job_id):
            return None
        return provider, parts[0].lower(), job_id.lower()
    except (ValueError, TypeError):
        return None


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError('ATS API redirect refused')


def get_json(url, timeout=15):
    p=urlparse(url)
    if p.scheme!='https' or p.hostname not in API_HOSTS or p.username or p.password or p.port not in (None,443):
        raise ValueError('not an allowed public ATS API')
    req=Request(url, headers={'User-Agent':'JobSearchVerifier/0.2 (public job boards)', 'Accept':'application/json'})
    with build_opener(NoRedirect()).open(req, timeout=timeout) as response:
        raw=response.read(25_000_001)
        if len(raw)>25_000_000:
            raise ValueError('ATS response exceeds 25 MB limit')
        return json.loads(raw)


def api_endpoint(provider, board):
    if not TOKEN.fullmatch(board):
        raise ValueError('invalid board name')
    board=quote(board.lower(),safe='')
    if provider=='ashby':
        return 'https://api.ashbyhq.com/posting-api/job-board/'+board+'?includeCompensation=true'
    if provider=='greenhouse':
        return 'https://boards-api.greenhouse.io/v1/boards/'+board+'/jobs?content=true'
    if provider in ('lever','lever_eu'):
        host='api.eu.lever.co' if provider=='lever_eu' else 'api.lever.co'
        return 'https://'+host+'/v0/postings/'+board+'?mode=json'
    raise ValueError('unsupported provider')


def plain(value):
    return unescape(re.sub('<[^>]+>', ' ', unescape(value or '')))


def normalize_job(provider, board, job):
    if not isinstance(job,dict):
        raise ValueError('non-object ATS record')
    job_url=job.get('jobUrl') if provider=='ashby' else job.get('absolute_url') if provider=='greenhouse' else job.get('hostedUrl')
    apply_url=job.get('applyUrl') if provider!='greenhouse' else job_url
    for link in (job_url, apply_url):
        if link:
            parsed=urlparse(link)
            if parsed.scheme!='https' or not parsed.hostname or parsed.username or parsed.password or parsed.port not in (None,443):
                raise ValueError('unsafe posting URL')
    url_ids=[identify(u) for u in (job_url,apply_url) if u]
    if provider=='ashby':
        if any(i is None or i[:2]!=(provider,board) for i in url_ids):
            raise ValueError('Ashby URL identity mismatch')
        ids={i[2] for i in url_ids}
        if len(ids)>1:
            raise ValueError('conflicting Ashby posting URLs')
        # URL ID is authoritative; some public feeds omit id or use an internal id.
        posting_id=next(iter(ids),str(job.get('id') or '').lower())
    else:
        posting_id=str(job.get('id') or '').lower()
        if provider.startswith('lever') and any(i != (provider,board,posting_id) for i in url_ids):
            raise ValueError('Lever URL identity mismatch')
    if not TOKEN.fullmatch(posting_id):
        raise ValueError('missing posting ID')
    title=job.get('title') if provider in ('ashby','greenhouse') else job.get('text')
    if not isinstance(title,str) or not title.strip():
        raise ValueError('missing posting title')
    comp=job.get('compensation') or {}
    salary_range=job.get('salaryRange') or {}
    if not isinstance(comp,dict) or not isinstance(salary_range,dict):
        raise ValueError('malformed compensation')
    salary=comp.get('scrapeableCompensationSalarySummary') or comp.get('compensationTierSummary')
    if not salary and salary_range:
        salary='{currency} {min}-{max} ({interval})'.format(**{k:salary_range.get(k,'') for k in ('currency','min','max','interval')})
    salary=salary or job.get('salaryDescriptionPlain')
    if provider=='greenhouse':
        location=(job.get('location') or {}).get('name')
        description=plain(job.get('content'))
    elif provider=='ashby':
        location=job.get('location')
        description=job.get('descriptionPlain') or plain(job.get('descriptionHtml'))
    else:
        location=(job.get('categories') or {}).get('location')
        description='\n'.join([job.get('descriptionPlain') or plain(job.get('description'))] +
            [str(x.get('text',''))+'\n'+plain(x.get('content')) for x in job.get('lists',[])] +
            [job.get('additionalPlain') or plain(job.get('additional')), job.get('salaryDescriptionPlain') or ''])
    return dict(provider=provider,board=board,posting_id=posting_id,title=title,location=location,
                salary_text=salary,job_url=job_url,apply_url=apply_url,description=description,
                workplace_type=job.get('workplaceType'),secondary_locations=job.get('secondaryLocations',[]),
                country=job.get('country'),address=job.get('address'),compensation=comp or salary_range,
                is_listed=job.get('isListed',True),published_at=job.get('publishedAt') or job.get('first_published'))


def retrieve_board(provider,board,fetch=get_json):
    board=board.lower()
    start=time.monotonic()
    result=dict(provider=provider,board=board,status='unverified',jobs=[],reason=None,
                checked_at=datetime.now(timezone.utc).isoformat(),endpoint=None)
    try:
        result['endpoint']=api_endpoint(provider,board)
        data=fetch(result['endpoint'])
        if provider in ('ashby','greenhouse'):
            if not isinstance(data,dict) or 'jobs' not in data or data.get('error'):
                raise ValueError('missing jobs array or API error')
            rows=data['jobs']
            if provider=='greenhouse' and isinstance(data.get('meta'),dict) and data['meta'].get('total',len(rows))!=len(rows):
                raise ValueError('incomplete Greenhouse board')
        else:
            rows=data
        if not isinstance(rows,list):
            raise ValueError('unexpected ATS response; expected list')
        result['jobs']=[normalize_job(provider,board,j) for j in rows]
        if len({j['posting_id'] for j in result['jobs']})!=len(rows):
            raise ValueError('duplicate posting IDs in board response')
        result['status']='published'
    except (HTTPError,URLError,TimeoutError,ValueError,OSError,TypeError,AttributeError,KeyError) as exc:
        result.update(jobs=[],reason='ATS API verification failed: '+str(exc))
    result['elapsed_seconds']=round(time.monotonic()-start,3)
    return result


def verify(url,fetch=get_json):
    identity=identify(url)
    base=dict(input_url=url,status='unverified',provider=None,board=None,posting_id=None,
              title=None,location=None,salary_text=None,job_url=None,apply_url=None,reason=None)
    if not identity:
        return dict(base,reason='unsupported or unsafe URL or job ID unavailable')
    provider,board,posting_id=identity
    base.update(provider=provider,board=board,posting_id=posting_id)
    result=retrieve_board(provider,board,fetch)
    base.update(checked_at=result['checked_at'],endpoint=result['endpoint'])
    if result['status']=='unverified':
        return dict(base,reason=result['reason'])
    for job in result['jobs']:
        if job['posting_id']==posting_id:
            return dict(base,**{k:v for k,v in job.items() if k not in base},
                        **{k:job[k] for k in ('title','location','salary_text','job_url','apply_url')},
                        status='published',reason='present on public ATS board; qualification and application acceptance require separate review')
    return dict(base,status='not_found',reason='posting ID absent from valid public board; may be unpublished, unlisted, or changed; not evidence of closure')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('url')
    p.add_argument('--json',action='store_true')
    args=p.parse_args()
    r=verify(args.url)
    print(json.dumps(r,indent=2,ensure_ascii=False))
    return {'published':0,'not_found':2,'unverified':3}[r['status']]

if __name__=='__main__': sys.exit(main())
