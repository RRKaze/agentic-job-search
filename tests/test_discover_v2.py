import csv
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).parents[1]/'scripts'))
import discover_v2 as d

class DiscoveryTests(unittest.TestCase):
    def test_ats_url_aliases(self):
        self.assertEqual(d.normalized_url('https://jobs.lever.co/Acme/abc/apply?source=x'), d.normalized_url('https://jobs.lever.co/acme/abc'))
        self.assertEqual(d.normalized_url('https://boards.greenhouse.io/acme/jobs/42'),d.normalized_url('https://job-boards.greenhouse.io/acme/jobs/42?gh_src=x'))
        self.assertNotEqual(d.normalized_url('https://example.com/job?id=1'),d.normalized_url('https://example.com/job?id=2'))

    def make_tracker(self,path):
        (path/'data/search-audits').mkdir(parents=True)
        (path/'data/jobs.csv').write_text('company,role,source_url,location\nAcme,Senior Backend,https://jobs.lever.co/acme/abc,US\n')
        (path/'data/search-audits/old.csv').write_text('company,role,source_url,location,reviewed_at,decision\nOther,Senior Backend,https://jobs.ashbyhq.com/other/id,US,2026-10-01,Hold\n')

    def test_canonical_and_prior_dedup(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp); self.make_tracker(p); index=d.load_seen(p)
            j={'job_url':'https://jobs.lever.co/acme/abc/apply','title':'Renamed','location':'US'}
            self.assertEqual(d.classify_seen(j,'Acme',index)[0],'already_tracked')
            j['job_url']='https://jobs.lever.co/acme/new'; j['title']='Senior Backend'
            self.assertEqual(d.classify_seen(j,'Acme',index)[0],'already_tracked')
            j['job_url']='https://jobs.ashbyhq.com/other/id'
            self.assertEqual(d.classify_seen(j,'Other',index)[0],'previously_seen')

    def test_fetch_once_per_board_default_hold(self):
        with tempfile.TemporaryDirectory() as tmp:
            p=Path(tmp); self.make_tracker(p)
            job={'job_url':'https://jobs.lever.co/new/id','title':'Senior Backend','location':'US'}
            board={'jobs':[job], 'status':'published','reason':None,'elapsed_seconds':0.1,'endpoint':'fixture','checked_at':'2026-10-15'}
            source=dict(provider='lever',board='new',company='New',bucket='remote',family='title',query='fixture')
            with patch.object(d,'retrieve_board',return_value=board) as fetch:
                result=d.run([source,source],p,p/'out','Senior')
            self.assertEqual(fetch.call_count,1)
            self.assertEqual(len(result['candidates']),1)
            self.assertEqual(result['candidates'][0]['decision'],'Hold')

    def test_apply_gate(self):
        job=dict(job_url='https://jobs.lever.co/new/id',apply_url='https://jobs.lever.co/new/id/apply',company='New',title='Senior Backend',location='US',seen='new',status='published',salary_text='USD 160000-190000',is_listed=True,checked_at='2026-10-15',evidence_sha256='abc',discovery_source='fixture')
        retrieval=dict(candidates=[job],run_id='fixture',reviewed_at='2026-10-15')
        review=dict(source_url=job['job_url'],decision='Apply',primary_reason='qualified',notes='reviewed',lead_bucket='remote')
        with self.assertRaises(ValueError): d.compile_audit(retrieval,[review])
        review.update(evidence_sha256='abc',location_country='US',base_salary_max_usd=190000,base_salary_min_usd=160000,base_salary_evidence='Fixture annual base')
        review['checks']={k:True for k in ('us_location','base_salary','technical_fit','seniority','eligibility','work_arrangement')}
        self.assertEqual(d.compile_audit(retrieval,[review])[0]['decision'],'Apply')
        job['seen']='already_tracked'
        self.assertEqual(d.compile_audit(retrieval,[review])[0]['decision'],'Skip')
        job['seen']='previously_seen'
        with self.assertRaises(ValueError): d.compile_audit(retrieval,[review])
        review['recheck_reason']='Previously blocked by JS; current API description reviewed'
        review['lead_bucket']='relocation_hybrid'
        with self.assertRaises(ValueError): d.compile_audit(retrieval,[review])
        review.update(onsite_cadence='3 days/week',office_city='New York',office_state='New York',relocation_required=True)
        self.assertEqual(d.compile_audit(retrieval,[review])[0]['decision'],'Apply')
        review['office_state']='CA'
        with self.assertRaises(ValueError): d.compile_audit(retrieval,[review])
        review['lead_bucket']='remote'
        review['base_salary_max_usd']=150000
        with self.assertRaises(ValueError): d.compile_audit(retrieval,[review])


class MissingPostingTests(unittest.TestCase):
    make_tracker=DiscoveryTests.make_tracker
    def test_absent_posting_and_failed_board_remain_hold(self):
        for state in ('published','unverified'):
            with tempfile.TemporaryDirectory() as tmp:
                p=Path(tmp); self.make_tracker(p)
                source=dict(provider='ashby',board='new',company='New',bucket='remote',family='title',query='fixture',posting_url='https://jobs.ashbyhq.com/new/absent',role='Senior Backend')
                board=dict(jobs=[],status=state,reason='failure' if state=='unverified' else None,elapsed_seconds=0,endpoint='fixture',checked_at='2026-10-15')
                with patch.object(d,'retrieve_board',return_value=board):
                    r=d.run([source],p,p/'out','Senior')
                self.assertEqual(r['candidates'][0]['decision'],'Hold')
                self.assertEqual(r['candidates'][0]['status'],'not_found' if state=='published' else 'unverified')

if __name__=='__main__': unittest.main()
