using System.Diagnostics;
using DevOps.Application.Abstractions;
using DevOps.Application.Monitoring;
using Microsoft.Extensions.Options;

namespace DevOps.Infrastructure.Monitoring;

public sealed class HttpHealthChecker(
    HttpClient httpClient,
    IOptions<MonitoringOptions> options) : IHttpHealthChecker
{
    public async Task<HealthProbeResult> CheckAsync(Uri healthUrl, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Value.RequestTimeout);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient.GetAsync(
                healthUrl,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);
            stopwatch.Stop();

            var message = response.IsSuccessStatusCode
                ? "Health endpoint returned a successful status."
                : $"Health endpoint returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.";

            return new HealthProbeResult(
                ReachedHost: true,
                IsSuccessStatusCode: response.IsSuccessStatusCode,
                HttpStatusCode: (int)response.StatusCode,
                ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                Message: message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or OperationCanceledException or NotSupportedException)
        {
            stopwatch.Stop();
            return new HealthProbeResult(
                ReachedHost: false,
                IsSuccessStatusCode: false,
                HttpStatusCode: null,
                ResponseTimeMs: stopwatch.ElapsedMilliseconds,
                Message: "Health endpoint was unreachable or timed out.");
        }
    }
}
