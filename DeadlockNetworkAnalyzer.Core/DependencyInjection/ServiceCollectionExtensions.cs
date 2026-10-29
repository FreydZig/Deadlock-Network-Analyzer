using DeadlockNetworkAnalyzer.Core.Abstractions;
using DeadlockNetworkAnalyzer.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeadlockNetworkAnalyzer.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeadlockNetworkAnalyzerCore(this IServiceCollection services)
    {
        services.AddSingleton<INetworkMetricsCollector, StubNetworkMetricsCollector>();
        services.AddSingleton<IGameDetectionService, StubGameDetectionService>();
        services.AddSingleton<IMonitoringService, MonitoringService>();

        return services;
    }
}
