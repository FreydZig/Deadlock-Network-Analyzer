namespace DeadlockNetworkAnalyzer.Core.Models;

public sealed record MonitoringSnapshot(
    MonitoringState State,
    GameStatus GameStatus,
    NetworkMetrics Metrics,
    string? StatusMessage = null);
