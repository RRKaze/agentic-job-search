"""Run: python3 -m unittest discover -s tests -p 'test_verify_ats.py'"""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("verify_ats", Path(__file__).parents[1] / "scripts" / "verify_ats.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)


class AtsVerifierTests(unittest.TestCase):
    def test_parse(self):
        self.assertEqual(mod.identify("https://jobs.ashbyhq.com/acme/abc123"), ("ashby", "acme", "abc123"))
        self.assertEqual(mod.identify("https://job-boards.greenhouse.io/acme/jobs/123"), ("greenhouse", "acme", "123"))
        self.assertEqual(mod.identify("https://jobs.lever.co/acme/123"), ("lever", "acme", "123"))
        self.assertIsNone(mod.identify("https://example.com/post"))

    def test_ashby_published(self):
        result = mod.verify("https://jobs.ashbyhq.com/acme/abcd", fetch=lambda _: {"jobs": [
            {"id": "abcd", "title": "Backend Engineer", "location": "Remote - US", "jobUrl": "https://jobs.ashbyhq.com/acme/abcd",
             "applyUrl": "https://jobs.ashbyhq.com/acme/abcd/application",
             "compensation": {"scrapeableCompensationSalarySummary": "$170,000-$200,000"}}
        ]})
        self.assertEqual(result["status"], "published")
        self.assertEqual(result["salary_text"], "$170,000-$200,000")

    def test_not_found_not_assumed_closed(self):
        r = mod.verify("https://jobs.lever.co/acme/abc", fetch=lambda _: [])
        self.assertEqual(r["status"], "not_found")
        self.assertIn("may be", r["reason"])

    def test_greenhouse(self):
        r = mod.verify("https://job-boards.greenhouse.io/acme/jobs/42",
                       fetch=lambda _: {"jobs": [{"id": 42, "title": "SWE III", "location": {"name": "NYC"}}]})
        self.assertEqual(r["status"], "published")
        self.assertEqual(r["location"], "NYC")

    def test_failure_is_unverified(self):
        def fail(_):
            raise TimeoutError("network")
        self.assertEqual(mod.verify("https://jobs.ashbyhq.com/acme/abcd", fetch=fail)["status"], "unverified")


if __name__ == "__main__":
    unittest.main()
