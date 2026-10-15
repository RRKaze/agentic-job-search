import sys
from pathlib import Path
import unittest
sys.path.insert(0, str(Path(__file__).parents[1] / 'scripts'))
import verify_ats as m

class Regressions(unittest.TestCase):
    def test_error_envelope_is_not_absence(self):
        self.assertEqual(m.verify('https://jobs.ashbyhq.com/acme/abc', lambda _: {'error':'denied'})['status'], 'unverified')

    def test_non_object_record_is_unverified(self):
        self.assertEqual(m.verify('https://jobs.lever.co/acme/abc', lambda _: ['invalid'])['status'], 'unverified')

    def test_ashby_url_id_without_id_field(self):
        rows=[{'title':'Backend', 'jobUrl':'https://jobs.ashbyhq.com/acme/abc', 'applyUrl':'https://jobs.ashbyhq.com/acme/abc/application'}]
        result=m.verify('https://jobs.ashbyhq.com/acme/abc', lambda _: {'jobs':rows})
        self.assertEqual(result['posting_id'], 'abc')
        self.assertEqual(result['status'], 'published')

    def test_ashby_cannot_match_other_board_url(self):
        rows=[{'title':'Backend', 'jobUrl':'https://jobs.ashbyhq.com/other/abc'}]
        self.assertEqual(m.verify('https://jobs.ashbyhq.com/acme/abc', lambda _: {'jobs':rows})['status'], 'unverified')

    def test_lever_eu_mapping(self):
        self.assertEqual(m.identify('https://jobs.eu.lever.co/acme/abc/apply'), ('lever_eu','acme','abc'))
        self.assertEqual(m.api_endpoint('lever_eu','acme'), 'https://api.eu.lever.co/v0/postings/acme?mode=json')

    def test_unsafe_inputs(self):
        for url in ['http://jobs.lever.co/acme/abc','https://x@jobs.lever.co/acme/abc','https://jobs.lever.co:444/acme/abc','https://jobs.lever.co/acme/abc/evil']:
            self.assertIsNone(m.identify(url))

    def test_lever_salary_and_urls(self):
        row={'id':'abc','text':'Senior Engineer','hostedUrl':'https://jobs.lever.co/acme/abc','applyUrl':'https://jobs.lever.co/acme/abc/apply','categories':{'location':'United States'},'salaryRange':{'min':160000,'max':190000,'currency':'USD','interval':'per-year-salary'}}
        r=m.verify(row['applyUrl'], lambda _: [row])
        self.assertEqual(r['job_url'],row['hostedUrl'])
        self.assertEqual(r['apply_url'],row['applyUrl'])
        self.assertIn('160000',r['salary_text'])


class AdditionalSafety(unittest.TestCase):
    def test_ashby_url_overrides_internal_id(self):
        row={'id':'internal','title':'Backend','jobUrl':'https://jobs.ashbyhq.com/acme/public'}
        self.assertEqual(m.verify(row['jobUrl'],lambda _: {'jobs':[row]})['posting_id'],'public')
        self.assertEqual(m.verify('https://jobs.ashbyhq.com/acme/internal',lambda _: {'jobs':[row]})['status'],'not_found')

    def test_encoded_greenhouse_html(self):
        self.assertEqual(m.plain('&lt;p&gt;Base salary $160,000&lt;/p&gt;').strip(),'Base salary $160,000')

    def test_redirect_refused(self):
        with self.assertRaises(ValueError):
            m.NoRedirect().redirect_request(None,None,302,'redirect',{},'http://127.0.0.1')

    def test_incomplete_board(self):
        r=m.verify('https://job-boards.greenhouse.io/acme/jobs/42',lambda _: {'jobs':[], 'meta':{'total':2}})
        self.assertEqual(r['status'],'unverified')

    def test_unknown_provider_rejected(self):
        with self.assertRaises(ValueError): m.api_endpoint('unknown','acme')

    def test_unsafe_response_link(self):
        r=m.verify('https://jobs.lever.co/acme/id',lambda _: [{'id':'id','text':'Job','hostedUrl':'http://jobs.lever.co/acme/id'}])
        self.assertEqual(r['status'],'unverified')

if __name__=='__main__': unittest.main()
