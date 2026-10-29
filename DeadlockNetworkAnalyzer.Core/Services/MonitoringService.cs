using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadlockNetworkAnalyzer.Core.Services;

public sealed class MonitoringService : IMonitoringService, IAsyncDisposable
{
    private readonly INetworkMetricsCollector _metricsCollector;
    private readonly IGameDetectionService _gameDetectionService;
    private readonly ILogger<MonitoringService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly object _sync = new();

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private MonitoringState _state = MonitoringState.Idle;

    public MonitoringService(
        INetworkMetricsCollector metricsCollector,
        IGameDetectionService gameDetectionService,
        ILogger<MonitoringService> logger,
        TimeSpan? pollInterval = null)
    {
        _metricsCollector = metricsCollector;
        _gameDetectionService = gameDetectionService;
        _logger = logger;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
    }

    public MonitoringState State
    {
        get
        {
            lock (_sync)
            {
                return _state;
            }
        }
        private set
        {
            lock (_sync)
            {
                _state = value;
            }
        }
    }

    public event EventHandler<MonitoringSnapshot>? SnapshotUpdated;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_state is MonitoringState.Running or MonitoringState.Starting)
            {
                return Task.CompletedTask;
            }

            State = MonitoringState.Starting;
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = RunLoopAsync(_loopCts.Token);
        }

        PublishSnapshot(
            MonitoringState.Starting,
            GameStatus.Unknown,
            NetworkMetrics.Empty,
            "Starting monitoring…");

        _logger.LogInformation("Monitoring started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Task? loopTask;
        CancellationTokenSource? loopCts;

        lock (_sync)
        {
            if (_state is MonitoringState.Idle or MonitoringState.Stopping)
            {
                return;
            }

            State = MonitoringState.Stopping;
            loopCts = _loopCts;
            loopTask = _loopTask;
            _loopCts = null;
            _loopTask = null;
        }

        PublishSnapshot(
            MonitoringState.Stopping,
            GameStatus.Unknown,
            NetworkMetrics.Empty,
            "Stopping monitoring…");

        if (loopCts is not null)
        {
            await loopCts.CancelAsync().ConfigureAwait(false);
        }

        if (loopTask is not null)
        {
            try
            {
                await loopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the monitoring loop is cancelled.
            }
        }

        loopCts?.Dispose();
        State = MonitoringState.Idle;

        PublishSnapshot(
            MonitoringState.Idle,
            GameStatus.NotRunning,
            NetworkMetrics.Empty,
            "Monitoring stopped.");

        _logger.LogInformation("Monitoring stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        State = MonitoringState.Running;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var gameStatus = await _gameDetectionService
                    .GetStatusAsync(cancellationToken)
                    .ConfigureAwait(false);

                var metrics = await _metricsCollector
                    .CollectAsync(cancellationToken)
                    .ConfigureAwait(false);

                PublishSnapshot(
                    MonitoringState.Running,
                    gameStatus,
                    metrics,
                    "Monitoring active (stub data).");

                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monitoring loop failed.");
            State = MonitoringState.Faulted;
            PublishSnapshot(
                MonitoringState.Faulted,
                GameStatus.Unknown,
                NetworkMetrics.Empty,
                ex.Message);
        }
    }

    private void PublishSnapshot(
        MonitoringState state,
        GameStatus gameStatus,
        NetworkMetrics metrics,
        string? statusMessage)
    {
        SnapshotUpdated?.Invoke(
            this,
            new MonitoringSnapshot(state, gameStatus, metrics, statusMessage));
    }
}
