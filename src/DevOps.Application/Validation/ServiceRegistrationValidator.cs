using System.Collections.ObjectModel;
using DevOps.Application.Common;

namespace DevOps.Application.Validation;

internal static class ServiceRegistrationValidator
{
    public static void Validate(string name, string? description, string baseUrl, string healthEndpoint, string? containerName = null)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Trim().Length > 200)
        {
            errors["name"] = ["Name must be 200 characters or fewer."];
        }

        if (description is { Length: > 2000 })
        {
            errors["description"] = ["Description must be 2000 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            errors["baseUrl"] = ["Base URL is required."];
        }
        else if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors["baseUrl"] = ["Base URL must be an absolute HTTP or HTTPS URL."];
        }

        if (string.IsNullOrWhiteSpace(healthEndpoint))
        {
            errors["healthEndpoint"] = ["Health endpoint is required."];
        }
        else         if (healthEndpoint.Trim().Length > 500)
        {
            errors["healthEndpoint"] = ["Health endpoint must be 500 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(containerName))
        {
            var trimmed = containerName.Trim();
            if (trimmed.Length > 128)
            {
                errors["containerName"] = ["Container name must be 128 characters or fewer."];
            }
            else if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains(' '))
            {
                errors["containerName"] = ["Container name must be a Docker container name, not a path."];
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(new ReadOnlyDictionary<string, string[]>(errors));
        }
    }
}
