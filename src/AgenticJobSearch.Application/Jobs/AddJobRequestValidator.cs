namespace AgenticJobSearch.Application.Jobs;

public static class AddJobRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(AddJobRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.SourceText))
        {
            errors["sourceText"] = ["A job description is required."];
        }
        else if (request.SourceText.Length > 20_000)
        {
            errors["sourceText"] = ["The job description cannot exceed 20,000 characters."];
        }

        AddMaximumLengthError(errors, "title", request.Title, 300);
        AddMaximumLengthError(errors, "company", request.Company, 300);
        AddMaximumLengthError(errors, "location", request.Location, 300);

        if (request.SourceUrl?.Length > 1_000)
        {
            errors["sourceUrl"] = ["The source URL cannot exceed 1,000 characters."];
        }
        else if (!string.IsNullOrWhiteSpace(request.SourceUrl)
                 && (!Uri.TryCreate(request.SourceUrl, UriKind.Absolute, out var sourceUri)
                     || (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps)))
        {
            errors["sourceUrl"] = ["The source URL must be an absolute HTTP or HTTPS URL."];
        }

        return errors;
    }

    private static void AddMaximumLengthError(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"The {field} cannot exceed {maximumLength} characters."];
        }
    }
}
