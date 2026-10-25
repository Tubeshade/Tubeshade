using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Testcontainers.Keycloak;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed class KeycloakFixture : IAsyncDisposable
{
    private const string Hostname = "keycloak";
    private const int PublicPort = 45279;

    private readonly KeycloakContainer _container;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _startTask;

    public INetwork Network { get; }

    public string ClientSecret { get; } = Guid.NewGuid().ToString("N");

    public string RealmBaseUrl => $"http://{Hostname}:{KeycloakBuilder.KeycloakPort}/realms/Test";

    public KeycloakFixture(string keycloakVersion)
    {
        Network = new NetworkBuilder().Build();

        _container = new KeycloakBuilder($"quay.io/keycloak/keycloak:{keycloakVersion}")
            .WithNetwork(Network)
            .WithHostname(Hostname)
            .WithPortBinding(PublicPort, KeycloakBuilder.KeycloakPort)
            .WithEnvironment("KC_HOSTNAME", $"http://localhost:{PublicPort}")
            .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true")
            .WithEnvironment("TUBESHADE_TEST_CLIENT_SECRET", ClientSecret)
            .WithRealm(PathHelper.GetRelativePath("./Keycloak/realm.json"))
            .Build();
    }

    /// <summary>Starts Keycloak without waiting for it, since only a few tests need it.</summary>
    public void Start() => _startTask = _container.StartAsync(_cancellationTokenSource.Token);

    public Task WaitUntilReadyAsync() => _startTask ?? throw new InvalidOperationException();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_startTask is not null)
        {
            await _cancellationTokenSource.CancelAsync();

            try
            {
                await _startTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        await _container.StopAsync();
        await Network.DeleteAsync();
        _cancellationTokenSource.Dispose();
    }
}
