using DevOps.Application.Monitoring;

namespace DevOps.UnitTests.Monitoring;

public class HealthUrlTests
{
    [Fact]
    public void Relative_slash_health_combines_with_http_base_url()
    {
        var url = HealthUrl.Combine("http://demo-unhealthy", "/health");

        Assert.Equal("http://demo-unhealthy/health", url.ToString());
        Assert.Equal(Uri.UriSchemeHttp, url.Scheme);
    }

    [Fact]
    public void Absolute_http_health_endpoint_is_used_as_is()
    {
        var url = HealthUrl.Combine("https://payments.internal", "https://payments.internal/ready");

        Assert.Equal("https://payments.internal/ready", url.ToString());
    }
}
