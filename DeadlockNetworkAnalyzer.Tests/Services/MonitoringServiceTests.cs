using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Models;
using DeadlockNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeadlockNetworkAnalyzer.Tests.Services;

public sealed class MonitoringServiceTests
{
    [Fact]
    public async Task StartAsync_PublishesRunningSnapshot_WithStubMetrics()
    {
        // Arrange
        await using var sut = CreateService(TimeSpan.FromMilliseconds(50));
        var snapshots = new List<MonitoringSnapshot>();
        sut.SnapshotUpdated += (_, snapshot) => snapshots.Add(snapshot);

        // Act
        await sut.StartAsync();
        await WaitForAsync(() => snapshots.Any(s => s.State == MonitoringState.Running), TimeSpan.FromSeconds(2));
        await sut.StopAsync();

        // Assert
        Assert.Contains(snapshots, s => s.State == MonitoringState.Starting);
        Assert.Contains(snapshots, s => s.State == MonitoringState.Running);
        Assert.Contains(snapshots, s => s.State == MonitoringState.Idle);

        var running = snapshots.First(s => s.State == MonitoringState.Running);
        Assert.Equal(GameStatus.NotRunning, running.GameStatus);
        Assert.Equal(0, running.Metrics.PingMilliseconds);
        Assert.Equal(0, running.Metrics.JitterMilliseconds);
        Assert.Equal(0, running.Metrics.PacketLossPercent);
        Assert.Equal(0, running.Metrics.TickMisses);
    }

    [Fact]
    public async Task StopAsync_WhenIdle_IsNoOp()
    {
        await using var sut = CreateService();

        await sut.StopAsync();

        Assert.Equal(MonitoringState.Idle, sut.State);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_DoesNotThrow()
    {
        await using var sut = CreateService(TimeSpan.FromMilliseconds(50));

        await sut.StartAsync();
        await sut.StartAsync();
        await sut.StopAsync();

        Assert.Equal(MonitoringState.Idle, sut.State);
    }

    [Fact]
    public async Task CollectAsync_RespectsCancellation()
    {
        var collector = new StubNetworkMetricsCollector(NullLogger<StubNetworkMetricsCollector>.Instance);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => collector.CollectAsync(cts.Token));
    }

    private static MonitoringService CreateService(TimeSpan? pollInterval = null)
    {
        return new MonitoringService(
            new StubNetworkMetricsCollector(NullLogger<StubNetworkMetricsCollector>.Instance),
            new StubGameDetectionService(NullLogger<StubGameDetectionService>.Instance),
            NullLogger<MonitoringService>.Instance,
            pollInterval);
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException("Condition was not met within the allotted time.");
            }

            await Task.Delay(20);
        }
    }
}
