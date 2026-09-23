using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgenticJobSearch.Application.Imports;
using AgenticJobSearch.Infrastructure.Imports;
using Npgsql;

public static class ImportEndpoints
{
    private const int MaxBytes = 5 * 1024 * 1024;
    public static void MapRecordImports(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/imports/job-records");
        group.AddEndpointFilter(async (context, next) =>
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            if (!config.GetValue<bool>("Imports:Enabled")) return Error(410, "IMPORT_RETIRED");
            var expected = config["Imports:Token"];
            var header = context.HttpContext.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(expected) || !header.StartsWith("Bearer ", StringComparison.Ordinal) ||
                !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(header[7..]))))
                return Error(401, "UNAUTHORIZED");
            try { return await next(context); }
            catch (ImportFailure ex) { return Error(ex.Status, ex.Code, ex.Details); }
            catch (NpgsqlException) { return Error(503, "DATABASE_UNAVAILABLE"); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Error(503, "DATABASE_UNAVAILABLE"); }
        });
        group.MapGet("/checkpoint", async (RecordImporter importer, CancellationToken ct) =>
            Results.Json(new { source_commit = await importer.CheckpointAsync(ct) }, ImportJson.Options));
        group.MapPost("", async (HttpRequest http, RecordImporter importer, CancellationToken ct) =>
        {
            if (http.ContentType?.Split(';')[0].Trim() != "application/json") return Error(400, "INVALID_CONTENT_TYPE");
            if (http.ContentLength > MaxBytes) return Error(413, "SIZE_LIMIT");
            using var buffer = new MemoryStream(); var bytes = new byte[8192]; int count;
            while ((count = await http.Body.ReadAsync(bytes, ct)) > 0)
            {
                if (buffer.Length + count > MaxBytes) return Error(413, "SIZE_LIMIT");
                await buffer.WriteAsync(bytes.AsMemory(0, count), ct);
            }
            ImportRequest? request;
            try
            {
                using var document = JsonDocument.Parse(buffer.ToArray());
                if (HasDuplicateKeys(document.RootElement)) return Error(400, "DUPLICATE_PROPERTY");
                request = document.RootElement.Deserialize<ImportRequest>(ImportJson.Options);
            }
            catch (JsonException) { return Error(400, "INVALID_REQUEST"); }
            if (request is null) return Error(400, "INVALID_REQUEST");
            return Results.Json(await importer.ImportAsync(request, ct), ImportJson.Options);
        });
    }
    private static bool HasDuplicateKeys(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>();
            foreach (var p in value.EnumerateObject()) if (!names.Add(p.Name) || HasDuplicateKeys(p.Value)) return true;
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) if (HasDuplicateKeys(item)) return true;
        return false;
    }
    private static IResult Error(int status, string code, IReadOnlyList<ImportIssue>? details = null) => Results.Json(new
    {
        error = new { code, message = "Import request failed.", details = (details ?? []).Take(100), truncated = (details?.Count ?? 0) > 100 }
    }, ImportJson.Options, statusCode: status);
}
