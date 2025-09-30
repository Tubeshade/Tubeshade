using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PubSubHubbub;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPubSubHubbubClient(this IServiceCollection services)
    {
        services
            .AddSingleton<IValidateOptions<PubSubHubbubOptions>, PubSubHubbubValidateOptions>()
            .AddOptions<PubSubHubbubOptions>()
            .BindConfiguration(PubSubHubbubOptions.SectionName);

        services
            .AddScoped<PubSubHubbubClient>(provider =>
            {
                var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(PubSubHubbubOptions.SectionName);
                return new(client);
            });

        services
            .AddHttpClient(PubSubHubbubOptions.SectionName, (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<PubSubHubbubOptions>>().CurrentValue;
                client.BaseAddress = options.BaseUrl;
            });

        return services;
    }
}
