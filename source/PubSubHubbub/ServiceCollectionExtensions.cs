using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Contrib.WaitAndRetry;

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
            })
            .AddTransientHttpErrorPolicy(builder => builder
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 3)));

        return services;
    }
}
