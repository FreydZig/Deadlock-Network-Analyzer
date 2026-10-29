using DeadlockNetworkAnalyzer.Core.Models;

namespace DeadlockNetworkAnalyzer.Core.Abstractions;

/// <summary>
/// Collects network quality metrics for Deadlock.
/// Real packet capture will be implemented in infrastructure later.
/// </summary>
public interface INetworkMetricsCollector
{
    Task<NetworkMetrics> CollectAsync(CancellationToken cancellationToken);
}
