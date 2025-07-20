using Microsoft.Extensions.DependencyInjection;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services) => services
        .AddHostedService<TaskListenerBackgroundService>()
        .AddHostedService<IndexBackgroundService>()
        .AddHostedService<DownloadBackgroundService>()
        .AddHostedService<ScanChannelBackgroundService>()
        .AddHostedService<ScanSubscriptionsBackgroundService>()
        .AddHostedService<SchedulerBackgroundService>();
}
