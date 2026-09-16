using System.Text.Json;
using DevOps.Application.Contracts;

namespace DevOps.Application.Analysis;

public static class IncidentAnalysisValidator
{
    public static bool TryParse(string? json, out IncidentAnalysisModelResult result, out string? error)
    {
        result = new IncidentAnalysisModelResult(false, null, "", "", "", [], [], [], [], "Malformed model output.");
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Model returned an empty body.";
            return false;
        }

        var candidate = ExtractJsonObject(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(candidate);
        }
        catch (JsonException exception)
        {
            error = $"Model output was not valid JSON: {exception.Message}";
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Model JSON was not an object.";
                return false;
            }

            var root = document.RootElement;
            var summary = ReadString(root, "summary");
            var probableCause = ReadString(root, "probableCause", "probable_cause");
            if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(probableCause))
            {
                error = "Model JSON was missing summary or probableCause.";
                return false;
            }

            result = new IncidentAnalysisModelResult(
                true,
                null,
                summary.Trim(),
                probableCause.Trim(),
                ReadString(root, "severityAssessment", "severity_assessment")?.Trim() ?? "Unknown",
                ReadStringList(root, "evidence"),
                ReadStringList(root, "recommendedChecks", "recommended_checks"),
                ReadStringList(root, "suggestedRemediation", "suggested_remediation"),
                ReadStringList(root, "limitations"),
                null);
            return true;
        }
    }

    internal static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var property in FindProperties(root, names))
        {
            var value = CoerceString(property);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static List<string> ReadStringList(JsonElement root, params string[] names)
    {
        foreach (var property in FindProperties(root, names))
        {
            return CoerceStringList(property);
        }

        return [];
    }

    private static IEnumerable<JsonElement> FindProperties(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var exact))
            {
                yield return exact;
            }
        }

        foreach (var property in root.EnumerateObject())
        {
            if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                yield return property.Value;
            }
        }
    }

    private static List<string> CoerceStringList(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => [],
            JsonValueKind.Array => element.EnumerateArray()
                .Select(CoerceString)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList(),
            _ => CoerceString(element) is { Length: > 0 } value ? [value] : []
        };
    }

    private static string? CoerceString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
            JsonValueKind.Array => string.Join("; ", element.EnumerateArray().Select(CoerceString).Where(value => !string.IsNullOrWhiteSpace(value))),
            JsonValueKind.Object => string.Join("; ", element.EnumerateObject().Select(property =>
            {
                var nested = CoerceString(property.Value);
                return string.IsNullOrWhiteSpace(nested) ? property.Name : $"{property.Name}: {nested}";
            })),
            _ => null
        };
    }
}
