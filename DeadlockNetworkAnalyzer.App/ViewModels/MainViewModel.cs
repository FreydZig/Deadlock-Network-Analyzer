using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadlockNetworkAnalyzer.App.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _gameStatusText = "Unknown";

    [ObservableProperty]
    private string _monitoringStatusText = "Idle";

    [ObservableProperty]
    private string _statusMessage = "Ready. Real data collection is not implemented yet.";

    [ObservableProperty]
    private string _pingText = "—";

    [ObservableProperty]
    private string _jitterText = "—";

    [ObservableProperty]
    private string _packetLossText = "—";

    [ObservableProperty]
    private string _tickMissesText = "—";

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _isBusy;

    public MainViewModel(IMonitoringService monitoringService, ILogger<MainViewModel> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
        _monitoringService.SnapshotUpdated += OnSnapshotUpdated;
    }

    [RelayCommand(CanExecute = nameof(CanStartMonitoring))]
    private async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            await _monitoringService.StartAsync(cancellationToken).ConfigureAwait(true);
            IsMonitoring = true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Start monitoring was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring.");
            StatusMessage = $"Failed to start: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopMonitoring))]
    private async Task StopMonitoringAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            await _monitoringService.StopAsync(cancellationToken).ConfigureAwait(true);
            IsMonitoring = false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stop monitoring was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring.");
            StatusMessage = $"Failed to stop: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStates();
        }
    }

    private bool CanStartMonitoring() => !IsMonitoring && !IsBusy;

    private bool CanStopMonitoring() => IsMonitoring && !IsBusy;

    partial void OnIsMonitoringChanged(bool value) => NotifyCommandStates();

    partial void OnIsBusyChanged(bool value) => NotifyCommandStates();

    private void NotifyCommandStates()
    {
        StartMonitoringCommand.NotifyCanExecuteChanged();
        StopMonitoringCommand.NotifyCanExecuteChanged();
    }

    private void OnSnapshotUpdated(object? sender, MonitoringSnapshot snapshot)
    {
        void Apply()
        {
            GameStatusText = FormatGameStatus(snapshot.GameStatus);
            MonitoringStatusText = snapshot.State.ToString();
            StatusMessage = snapshot.StatusMessage ?? string.Empty;

            if (snapshot.State == MonitoringState.Running)
            {
                PingText = $"{snapshot.Metrics.PingMilliseconds:0.0} ms";
                JitterText = $"{snapshot.Metrics.JitterMilliseconds:0.0} ms";
                PacketLossText = $"{snapshot.Metrics.PacketLossPercent:0.00} %";
                TickMissesText = snapshot.Metrics.TickMisses.ToString();
                IsMonitoring = true;
            }
            else if (snapshot.State is MonitoringState.Idle or MonitoringState.Faulted)
            {
                PingText = "—";
                JitterText = "—";
                PacketLossText = "—";
                TickMissesText = "—";
                IsMonitoring = false;
            }
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Apply();
        }
        else
        {
            dispatcher.Invoke(Apply);
        }
    }

    private static string FormatGameStatus(GameStatus status) => status switch
    {
        GameStatus.NotRunning => "Not Running",
        GameStatus.Running => "Running",
        GameStatus.InMatch => "In Match",
        _ => "Unknown"
    };

    public void Dispose()
    {
        _monitoringService.SnapshotUpdated -= OnSnapshotUpdated;
    }
}
