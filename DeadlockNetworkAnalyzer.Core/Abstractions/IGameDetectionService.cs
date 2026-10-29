using DeadlockNetworkAnalyzer.Core.Models;

namespace DeadlockNetworkAnalyzer.Core.Abstractions;

/// <summary>
/// Detects whether Deadlock is running / in a match.
/// Process inspection will be implemented in infrastructure later.
/// </summary>
public interface IGameDetectionService
{
    Task<GameStatus> GetStatusAsync(CancellationToken cancellationToken);
}
