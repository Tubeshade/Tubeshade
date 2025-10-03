using DbUp.Engine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

public sealed class TubeshadeApplicationFactory : WebApplicationFactory<HumanReadablePeriodPattern>
{
    private readonly IConfiguration _configuration;

    public TubeshadeApplicationFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddConfiguration(_configuration))
            .ConfigureServices(services => services
                .AddSingleton<IScriptPreprocessor, UnloggedTableScriptPreprocessor>()
                .AddScoped<IYtdlpWrapper, MockYtdlpWrapper>());
    }

    /// <inheritdoc />
    /// <seealso href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-UNLOGGED"/>
    private sealed class UnloggedTableScriptPreprocessor : IScriptPreprocessor
    {
        /// <inheritdoc />
        public string Process(string contents) => contents.Replace("CREATE TABLE", "CREATE UNLOGGED TABLE");
    }
}
