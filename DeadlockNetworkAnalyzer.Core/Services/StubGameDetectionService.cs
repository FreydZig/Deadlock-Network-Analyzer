using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadlockNetworkAnalyzer.Core.Services;

/// <summary>
/// Placeholder game detection. Always reports NotRunning until process detection exists.
/// </summary>
public sealed class StubGameDetectionService : IGameDetectionService
{
    private readonly ILogger<StubGameDetectionService> _logger;

    public StubGameDetectionService(ILogger<StubGameDetectionService> logger)
    {
        _logger = logger;
    }

    public Task<GameStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Resolving stub game status.");

        return Task.FromResult(GameStatus.NotRunning);
    }
}
