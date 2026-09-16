using DevOps.Core.Entities;
using DevOps.Core.Enums;

namespace DevOps.Application.Analysis;

public static class IncidentSimilarityScorer
{
    public static double Score(Incident current, Incident historical)
    {
        var left = Tokenize($"{current.Title} {current.Description}");
        var right = Tokenize($"{historical.Title} {historical.Description} {historical.RootCause} {historical.Resolution} {historical.ActionsTaken}");
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var intersection = left.Intersect(right, StringComparer.OrdinalIgnoreCase).Count();
        var union = left.Union(right, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    public static IReadOnlyList<(Incident Incident, double Score)> Rank(
        Incident current,
        IEnumerable<Incident> candidates,
        double minimumScore,
        int take)
    {
        return candidates
            .Where(candidate => candidate.Id != current.Id && candidate.Status == IncidentStatus.Resolved)
            .Select(candidate => (Incident: candidate, Score: Score(current, candidate)))
            .Where(item => item.Score >= minimumScore)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Incident.DetectedAt)
            .Take(take)
            .ToArray();
    }

    private static HashSet<string> Tokenize(string? text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return tokens;
        }

        foreach (var raw in text.Split([' ', ',', '.', ';', ':', '/', '\\', '-', '_', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            var token = raw.Trim().ToLowerInvariant();
            if (token.Length < 3 || StopWords.Contains(token))
            {
                continue;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "that", "this", "was", "were", "are", "has", "have"
    };
}
