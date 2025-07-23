using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SponsorBlock.Tests.Integration;

public static class TestConfiguration
{
    public static IServiceProvider Services { get; }

    static TestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        Services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddSimpleConsole())
            .AddSponsorBlockClient()
            .BuildServiceProvider(true);
    }
}
