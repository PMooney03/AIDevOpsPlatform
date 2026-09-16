using DevOps.Application.Common;

namespace DevOps.Application.Validation;

internal static class IncidentValidator
{
    public static void Validate(string title, string? description)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Trim().Length > 300)
        {
            errors["title"] = ["Title must be 300 characters or fewer."];
        }

        if (description is { Length: > 4000 })
        {
            errors["description"] = ["Description must be 4000 characters or fewer."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }
    }
}
