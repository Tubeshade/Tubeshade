using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;
using Testcontainers.Keycloak;
using Testcontainers.PostgreSql;
using Tubeshade.Server.Configuration;
using Tubeshade.Server.Configuration.Auth.Options;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed partial class ServerFixture : IServerFixture
{
    public const string TestDirectory = "/test";
    private const string KeycloakHostname = "keycloak";

    private static int _keycloakPublicPort = 45278;

    private readonly bool _shutdown;
    private readonly INetwork _network;
    private readonly List<IContainer> _containers;
    private readonly IContainer _serverContainer;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Uri BaseAddress => new UriBuilder
    {
        Scheme = "http",
        Host = "localhost",
        Port = _serverContainer.GetMappedPublicPort(),
    }.Uri;

    /// <inheritdoc />
    public HttpClient HttpClient => new() { BaseAddress = BaseAddress };

    public ServerFixture(string name, string serverImageName, string postgresqlVersion, string keycloakVersion, bool shutdown)
    {
        _shutdown = shutdown;
        Name = name;

        _network = new NetworkBuilder().Build();

        var databaseContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:{postgresqlVersion}")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithNetwork(_network)
            .WithHostname("database")
            .WithPortBinding(PostgreSqlBuilder.PostgreSqlPort, true)
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        var clientSecret = Guid.NewGuid().ToString("N");
        var realmFile = PathHelper.GetRelativePath("./Keycloak/realm.json");
        var publicPort = Interlocked.Increment(ref _keycloakPublicPort);

        var keycloakContainer = new KeycloakBuilder()
            .WithImage($"quay.io/keycloak/keycloak:{keycloakVersion}")
            .WithNetwork(_network)
            .WithHostname(KeycloakHostname)
            .WithPortBinding(publicPort, KeycloakBuilder.KeycloakPort)
            .WithEnvironment("KC_HOSTNAME", $"http://localhost:{publicPort}")
            .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
            .WithEnvironment("TUBESHADE_TEST_CLIENT_SECRET", clientSecret)
            .WithRealm(realmFile)
            .Build();

        var reportsDirectory = Path.Combine(CommonDirectoryPath.GetProjectDirectory().DirectoryPath, "TestResults");
        if (!Directory.Exists(reportsDirectory))
        {
            Directory.CreateDirectory(reportsDirectory);
        }

        var keycloakRealmBaseUrl = $"http://{KeycloakHostname}:{KeycloakBuilder.KeycloakPort}/realms/Test";
        _serverContainer = new ContainerBuilder()
            .WithImage(serverImageName)
            .WithNetwork(_network)
            .WithBindMount(reportsDirectory, "/reports", AccessMode.ReadWrite)
            .WithTmpfsMount(TestDirectory)
            .WithEnvironment("Database__ConnectionString", $"Host=database; Port={PostgreSqlBuilder.PostgreSqlPort}; Username=postgres; Password=postgres; Include Error Detail=true")
            .WithEnvironment($"{SchedulerOptions.SectionName}__{nameof(SchedulerOptions.Period)}", "PT5S")
            .WithEnvironment($"{OidcProviderOptions.SectionName}__Keycloak__ServerRealm", keycloakRealmBaseUrl)
            .WithEnvironment($"{OidcProviderOptions.SectionName}__Keycloak__Metadata", $"{keycloakRealmBaseUrl}/.well-known/openid-configuration")
            .WithEnvironment($"{OidcProviderOptions.SectionName}__Keycloak__ClientId", "tubeshade")
            .WithEnvironment($"{OidcProviderOptions.SectionName}__Keycloak__ClientSecret", clientSecret)
            .WithEnvironment($"{OidcProviderOptions.SectionName}__Keycloak__RequireHttpsMetadata", "false")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8080).UntilMessageIsLogged(StartedRegex()))
            .DependsOn(databaseContainer)
            .DependsOn(keycloakContainer)
            .Build();

        _containers = [databaseContainer, keycloakContainer, _serverContainer];
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync(cancellationToken)));

        // the release image runs with a non-root user, which does not have write access to mounted paths
        // weirdly, this only is needed in GitHub actions and not locally
        using var client = new DockerClientConfiguration().CreateClient();
        var createResponse = await client.Exec.CreateContainerExecAsync(
            _serverContainer.Id,
            new ContainerExecCreateParameters
            {
                User = "root",
                Cmd = ["chown", "app:app", TestDirectory],
            },
            cancellationToken);

        var stream = await client.Exec.StartContainerExecAsync(createResponse.ID, new(), cancellationToken);
        var (output, error) = await stream.ReadOutputToEndAsync(cancellationToken);
        await TestContext.Out.WriteLineAsync(output);
        await TestContext.Out.WriteLineAsync(error);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_shutdown)
        {
            try
            {
                using var httpClient = HttpClient;
                using var response = await httpClient.GetAsync("/api/v1.0/Stop");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            _ = await _serverContainer.GetExitCodeAsync();
        }

        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
        await _network.DeleteAsync();
    }

    [GeneratedRegex(".*Now listening on.*")]
    private static partial Regex StartedRegex();
}
