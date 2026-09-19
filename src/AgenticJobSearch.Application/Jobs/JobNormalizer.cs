using AgenticJobSearch.Domain;

namespace AgenticJobSearch.Application.Jobs;

public sealed class JobNormalizer
{
    public Job Normalize(AddJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceText))
        {
            throw new ArgumentException("Job text is required.", nameof(request));
        }

        var source = request.SourceText.Trim();

        return new Job
        {
            Id = Guid.NewGuid(),
            SourceText = source,
            SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            Title = FirstNonEmpty(request.Title, ExtractLabeledValue(source, "title"), InferTitle(source)),
            Company = FirstNonEmpty(request.Company, ExtractLabeledValue(source, "company"), "Unknown company"),
            Location = FirstNonEmpty(request.Location, ExtractLabeledValue(source, "location"), InferLocation(source)),
            Seniority = InferSeniority(source),
            WorkMode = InferWorkMode(source),
            LifecycleState = JobLifecycleState.Discovered,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    internal static bool ContainsAny(string source, params string[] terms)
    {
        return terms.Any(term => source.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string? ExtractLabeledValue(string source, string label)
    {
        var line = source
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(item => item.StartsWith($"{label}:", StringComparison.OrdinalIgnoreCase));

        return line?.Split(':', 2)[1].Trim();
    }

    private static string InferTitle(string source)
    {
        var firstLine = source
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(firstLine) && firstLine.Length <= 90)
        {
            return firstLine;
        }

        return ContainsAny(source, "staff engineer", "principal engineer")
            ? "Senior-adjacent engineering role"
            : "Software engineering role";
    }

    private static string InferLocation(string source)
    {
        return ContainsAny(source, "remote", "united states", "u.s.", "us remote")
            ? "United States / Remote"
            : "Unknown location";
    }

    private static string InferSeniority(string source)
    {
        if (ContainsAny(source, "senior", "sr.", "swe iii", "software engineer iii"))
        {
            return "Senior";
        }

        if (ContainsAny(source, "staff", "principal"))
        {
            return "Staff+";
        }

        if (ContainsAny(source, "junior", "entry level", "new grad"))
        {
            return "Junior";
        }

        return "Unspecified";
    }

    private static JobWorkMode InferWorkMode(string source)
    {
        if (ContainsAny(source, "remote"))
        {
            return JobWorkMode.Remote;
        }

        if (ContainsAny(source, "hybrid"))
        {
            return JobWorkMode.Hybrid;
        }

        if (ContainsAny(source, "onsite", "on-site"))
        {
            return JobWorkMode.Onsite;
        }

        return JobWorkMode.Unknown;
    }
}
