using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;

namespace DevOps.UnitTests.Fakes;

internal sealed class FakeHttpHealthChecker : IHttpHealthChecker
{
    private readonly Queue<HealthProbeResult> _results = new();
    public List<Uri> RequestedUrls { get; } = [];

    public void Enqueue(HealthProbeResult result) => _results.Enqueue(result);

    public Task<HealthProbeResult> CheckAsync(Uri healthUrl, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(healthUrl);
        if (_results.Count == 0)
        {
            throw new InvalidOperationException("No health probe result was queued for this test.");
        }

        return Task.FromResult(_results.Dequeue());
    }
}
