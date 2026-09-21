using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgenticJobSearch.Application.Imports;
using AgenticJobSearch.Domain;
using AgenticJobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AgenticJobSearch.Infrastructure.Imports;

public sealed class RecordImporter(JobSearchDbContext db, ISourceHeadVerifier verifier)
{
    public const string Repository = "RRKaze/job-search";
    public const string SourceKey = Repository + ":main";
    public async Task<string?> CheckpointAsync(CancellationToken ct) =>
        await db.ImportCheckpoints.Where(x => x.Source == SourceKey).Select(x => x.Commit).SingleOrDefaultAsync(ct);

    public async Task<object> ImportAsync(ImportRequest request, CancellationToken ct)
    {
        var issues = ImportValidation.Validate(request);
        if (request.Source.Repository != Repository || request.Source.Branch != "main") throw new ImportFailure(403, "UNAUTHORIZED_SOURCE");
        if (issues.Count > 0) throw new ImportFailure(422, "VALIDATION_FAILED", issues);
        var head = await verifier.GetHeadAsync(ct);
        if (head != request.Source.CommitSha) throw new ImportFailure(409, "STALE_SOURCE");
        var canonical = JsonSerializer.Serialize(new
        {
            request.SchemaVersion,
            request.Source.Repository,
            request.Source.Branch,
            request.Source.CommitSha,
            Jobs = request.Jobs.OrderBy(x => x["job_id"], StringComparer.Ordinal).Select(ImportValidation.RowJson),
            Applications = request.Applications.OrderBy(x => x["application_id"], StringComparer.Ordinal).Select(ImportValidation.RowJson)
        });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A fixed transaction-scoped lock serializes this single configured import source across processes.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(721493811)", ct);
        var checkpoint = await db.ImportCheckpoints.SingleOrDefaultAsync(x => x.Source == SourceKey, ct);
        var applicationJobIds = request.Applications.Select(a => a["job_id"]).ToHashSet(StringComparer.Ordinal);
        object Result(string result, ImportCounts jobs, ImportCounts applications) => new
        {
            schema_version = "1.0",
            result,
            source_commit = request.Source.CommitSha,
            dry_run = request.DryRun,
            counts = new { jobs, applications },
            warnings = request.Jobs.Select((j, i) => (j, i)).Where(x =>
                new[] { "applied", "interviewing", "offer" }.Contains(x.j["stage"]) && !applicationJobIds.Contains(x.j["job_id"]))
                .Select(x => new ImportIssue("jobs", x.i + 1, "stage", "MISSING_APPLICATION")).ToArray()
        };
        if (checkpoint?.Commit == request.Source.CommitSha)
        {
            if (checkpoint.PayloadHash != hash) throw new ImportFailure(409, "SOURCE_CONTENT_CONFLICT");
            return Result(request.DryRun ? "validated" : "already_imported", new(0, 0, request.Jobs.Count), new(0, 0, request.Applications.Count));
        }
        if (checkpoint?.Commit != request.Source.ExpectedPreviousCommit) throw new ImportFailure(409, "CHECKPOINT_CONFLICT");
        var jobs = await db.Jobs.Where(x => x.SourceRepository == Repository).ToDictionaryAsync(x => x.ExternalId!, ct);
        var apps = await db.Applications.Where(x => x.SourceRepository == Repository).ToDictionaryAsync(x => x.ExternalId!, ct);
        if (jobs.Keys.Except(request.Jobs.Select(x => x["job_id"]!)).Any() || apps.Keys.Except(request.Applications.Select(x => x["application_id"]!)).Any())
            throw new ImportFailure(409, "SOURCE_RECORD_MISSING");
        foreach (var a in request.Applications)
            if (apps.TryGetValue(a["application_id"]!, out var existing) && existing.JobId != jobs.GetValueOrDefault(a["job_id"]!)?.Id)
                throw new ImportFailure(409, "APPLICATION_LINK_CHANGED");
        var now = DateTimeOffset.UtcNow;
        void History(string entity, string id, string? previous, string current)
        {
            if (previous == current) return;
            db.TrackingChanges.Add(new() { Id = Guid.NewGuid(), Source = SourceKey, Entity = entity, ExternalId = id, Previous = previous, Current = current, Commit = request.Source.CommitSha, ObservedAt = now });
        }
        bool Same(string? existing, string canonicalRow) => existing is not null && ImportValidation.RowJson(JsonSerializer.Deserialize<Dictionary<string, string?>>(existing)!) == canonicalRow;
        DateOnly? Date(string? value) => value is null ? null : DateOnly.ParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        int ji = 0, ju = 0, jc = 0, ai = 0, au = 0, ac = 0;
        foreach (var row in request.Jobs)
        {
            var id = row["job_id"]!; var json = ImportValidation.RowJson(row);
            if (!jobs.TryGetValue(id, out var job))
            {
                job = new() { Id = Guid.NewGuid(), SourceRepository = Repository, ExternalId = id, CreatedAt = now };
                jobs.Add(id, job); db.Jobs.Add(job); ji++;
            }
            else if (Same(job.ImportedRecord, json)) { jc++; continue; } else ju++;
            History("jobs", id, job.TrackingStage, row["stage"]!);
            job.Title = row["role"]!; job.Company = row["company"]!; job.Location = row["location"]!; job.SourceUrl = row["source_url"];
            job.SalaryText = row["salary_text"]; job.Priority = row["priority"]; job.FitRationale = row["fit_rationale"]; job.GapsNotes = row["gaps_notes"];
            job.StatusDate = Date(row["status_date"]); job.VerifiedDate = Date(row["verified_date"]);
            job.TrackingStage = row["stage"]; job.ImportedRecord = json; job.UpdatedAt = now;
        }
        foreach (var row in request.Applications)
        {
            var id = row["application_id"]!; var json = ImportValidation.RowJson(row);
            if (!apps.TryGetValue(id, out var app))
            {
                app = new() { Id = Guid.NewGuid(), SourceRepository = Repository, ExternalId = id, JobId = jobs[row["job_id"]!].Id, CreatedAt = now };
                db.Applications.Add(app); ai++;
            }
            else if (Same(app.ImportedRecord, json)) { ac++; continue; } else au++;
            var previous = app.ImportedRecord is null ? null : JsonSerializer.Deserialize<Dictionary<string, string?>>(app.ImportedRecord)!["status"];
            History("applications", id, previous, row["status"]!);
            app.State = row["status"] switch { "submitted" => ApplicationState.Submitted, "interviewing" => ApplicationState.Interview, "offer" => ApplicationState.Offer, "rejected" => ApplicationState.Rejected, _ => ApplicationState.Withdrawn };
            // Preserve the date-only source in ImportedRecord; do not fabricate a submission time.
            app.SubmittedDate = Date(row["submitted_date"]); app.ResumeVersion = row["resume_version"]; app.ReferralContact = row["referral_contact"];
            app.NextFollowUp = Date(row["next_follow_up"]); app.Outcome = row["outcome"]; app.Notes = row["notes"];
            app.ImportedRecord = json;
        }
        if (!request.DryRun)
        {
            if (checkpoint is null) { checkpoint = new() { Source = SourceKey }; db.ImportCheckpoints.Add(checkpoint); }
            checkpoint.Commit = request.Source.CommitSha; checkpoint.PayloadHash = hash; checkpoint.ImportedAt = now;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        }
        else db.ChangeTracker.Clear();
        return Result(request.DryRun ? "validated" : "imported", new(ji, ju, jc), new(ai, au, ac));
    }
}
