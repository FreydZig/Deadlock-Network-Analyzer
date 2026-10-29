using DeadlockNetworkAnalyzer.Core.Models;

namespace DeadlockNetworkAnalyzer.Core.Abstractions;

public interface IMonitoringService
{
    MonitoringState State { get; }

    event EventHandler<MonitoringSnapshot>? SnapshotUpdated;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
