namespace DevOps.Application.Monitoring;

public static class HealthUrl
{
    public static Uri Combine(string baseUrl, string healthEndpoint)
    {
        if (Uri.TryCreate(healthEndpoint, UriKind.Absolute, out var absoluteEndpoint) &&
            (absoluteEndpoint.Scheme == Uri.UriSchemeHttp || absoluteEndpoint.Scheme == Uri.UriSchemeHttps))
        {
            return absoluteEndpoint;
        }

        var normalisedBase = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        var relative = healthEndpoint.TrimStart('/');
        return new Uri(new Uri(normalisedBase, UriKind.Absolute), relative);
    }
}
