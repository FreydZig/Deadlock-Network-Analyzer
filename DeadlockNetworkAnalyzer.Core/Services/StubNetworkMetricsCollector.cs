using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadlockNetworkAnalyzer.Core.Services;

/// <summary>
/// Placeholder metrics collector. Returns zeros until real capture is wired up.
/// </summary>
public sealed class StubNetworkMetricsCollector : INetworkMetricsCollector
{
    private readonly ILogger<StubNetworkMetricsCollector> _logger;

    public StubNetworkMetricsCollector(ILogger<StubNetworkMetricsCollector> logger)
    {
        _logger = logger;
    }

    public Task<NetworkMetrics> CollectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Collecting stub network metrics.");

        var metrics = new NetworkMetrics(
            PingMilliseconds: 0,
            JitterMilliseconds: 0,
            PacketLossPercent: 0,
            TickMisses: 0,
            CapturedAt: DateTimeOffset.UtcNow);

        return Task.FromResult(metrics);
    }
}
