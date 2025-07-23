using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SponsorBlock;

public static class ServiceCollectionExtensions
{
    public const string HttpClientName = "SponsorBlock";

    public static IServiceCollection AddSponsorBlockClient(this IServiceCollection services)
    {
        services
            .AddSingleton<IValidateOptions<SponsorBlockOptions>, SponsorBlockValidateOptions>()
            .AddOptions<SponsorBlockOptions>()
            .BindConfiguration(HttpClientName);

        services
            .AddScoped<ISponsorBlockClient, SponsorBlockClient>(provider =>
            {
                var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
                return new(client);
            });

        services
            .AddHttpClient(HttpClientName, (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<SponsorBlockOptions>>().CurrentValue;
                client.BaseAddress = new Uri(options.BaseUrl);
            });

        return services;
    }
}
