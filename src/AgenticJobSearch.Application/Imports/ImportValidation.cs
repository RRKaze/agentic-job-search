using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace AgenticJobSearch.Application.Imports;

public static class ImportValidation
{
    public static readonly string[] JobFields = "job_id company role location salary_text priority fit_rationale gaps_notes stage status_date source_url verified_date".Split(' ');
    public static readonly string[] ApplicationFields = "application_id job_id submitted_date status resume_version referral_contact next_follow_up outcome notes".Split(' ');
    public static readonly Dictionary<string, string> Stages = new() { ["submitted"] = "applied", ["interviewing"] = "interviewing", ["offer"] = "offer", ["rejected"] = "rejected", ["withdrawn"] = "withdrawn" };
    public static List<ImportIssue> Validate(ImportRequest r)
    {
        if (r.SchemaVersion != "1.0" || r.Source is null || r.Jobs is null || r.Applications is null ||
            !Regex.IsMatch(r.Source.CommitSha ?? "", "^[0-9a-f]{40}$") ||
            (r.Source.ExpectedPreviousCommit is not null && !Regex.IsMatch(r.Source.ExpectedPreviousCommit, "^[0-9a-f]{40}$")))
            throw new ImportFailure(400, "INVALID_REQUEST");
        if (r.Jobs.Count > 10000 || r.Applications.Count > 10000) throw new ImportFailure(413, "SIZE_LIMIT");
        var errors = new List<ImportIssue>();
        void Check(List<Dictionary<string, string?>> rows, bool jobs)
        {
            var fields = jobs ? JobFields : ApplicationFields;
            var required = (jobs ? "job_id company role location priority stage source_url verified_date" : "application_id job_id submitted_date status").Split(' ');
            var ids = new HashSet<string>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; var entity = jobs ? "jobs" : "applications";
                void Error(string field, string code) => errors.Add(new(entity, i + 1, field, code));
                if (row is null) { Error("", "INVALID_ROW"); continue; }
                foreach (var f in fields) if (!row.ContainsKey(f)) Error(f, "MISSING_FIELD");
                foreach (var f in row.Keys) if (!fields.Contains(f)) Error(f, "UNKNOWN_FIELD");
                foreach (var f in required) if (string.IsNullOrWhiteSpace(row.GetValueOrDefault(f))) Error(f, "REQUIRED");
                foreach (var (f, v) in row)
                {
                    if (v is null) continue;
                    if (v.Length == 0) Error(f, "EMPTY_USE_NULL");
                    int max = f.EndsWith("_id") ? 128 : f == "source_url" ? 2048 : f == "priority" ? 200 : new[] { "company", "role", "location" }.Contains(f) ? 500 : new[] { "fit_rationale", "gaps_notes", "notes" }.Contains(f) ? 20000 : 2000;
                    if (v.Length > max) Error(f, "TOO_LONG");
                    if (new[] { "status_date", "verified_date", "submitted_date", "next_follow_up" }.Contains(f) && !DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) Error(f, "INVALID_DATE");
                }
                var id = row.GetValueOrDefault(jobs ? "job_id" : "application_id");
                if (id is not null && !ids.Add(id)) Error(jobs ? "job_id" : "application_id", "DUPLICATE_ID");
                if (jobs)
                {
                    if (!new[] { "lead", "applied", "interviewing", "offer", "rejected", "withdrawn", "unavailable", "ineligible" }.Contains(row.GetValueOrDefault("stage"))) Error("stage", "INVALID_STAGE");
                    if (!Uri.TryCreate(row.GetValueOrDefault("source_url"), UriKind.Absolute, out var url) || url.Scheme != "https" || string.IsNullOrEmpty(url.Host) || url.UserInfo != "") Error("source_url", "INVALID_URL");
                }
                else
                {
                    if (!Stages.ContainsKey(row.GetValueOrDefault("status") ?? "")) Error("status", "INVALID_STATUS");
                    if (row.GetValueOrDefault("status") == "rejected" && row.GetValueOrDefault("outcome") != "rejected") Error("outcome", "INVALID_OUTCOME");
                    if (new[] { "rejected", "withdrawn" }.Contains(row.GetValueOrDefault("status")) && row.GetValueOrDefault("next_follow_up") is not null) Error("next_follow_up", "CLOSED_FOLLOW_UP");
                }
            }
        }
        Check(r.Jobs, true); Check(r.Applications, false);
        if (errors.Count > 0) return errors;
        var jobs = r.Jobs.ToDictionary(x => x["job_id"]!);
        var pairs = new HashSet<(string?, string?)>();
        for (var i = 0; i < r.Jobs.Count; i++) if (!pairs.Add((r.Jobs[i]["company"], r.Jobs[i]["role"]))) errors.Add(new("jobs", i + 1, "role", "DUPLICATE_COMPANY_ROLE"));
        var links = new HashSet<string>();
        for (var i = 0; i < r.Applications.Count; i++)
        {
            var a = r.Applications[i];
            if (!links.Add(a["job_id"]!)) errors.Add(new("applications", i + 1, "job_id", "DUPLICATE_APPLICATION"));
            if (!jobs.TryGetValue(a["job_id"]!, out var job)) errors.Add(new("applications", i + 1, "job_id", "UNKNOWN_JOB"));
            else if (job["stage"] != Stages[a["status"]!]) errors.Add(new("applications", i + 1, "status", "STATUS_MISMATCH"));
        }
        return errors;
    }
    public static string RowJson(Dictionary<string, string?> row) => JsonSerializer.Serialize(row.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value));
}
