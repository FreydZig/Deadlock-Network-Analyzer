namespace DeadlockNetworkAnalyzer.Core.Models;

public sealed record NetworkMetrics(
    double PingMilliseconds,
    double JitterMilliseconds,
    double PacketLossPercent,
    int TickMisses,
    DateTimeOffset CapturedAt)
{
    public static NetworkMetrics Empty { get; } = new(
        PingMilliseconds: 0,
        JitterMilliseconds: 0,
        PacketLossPercent: 0,
        TickMisses: 0,
        CapturedAt: DateTimeOffset.MinValue);
}
