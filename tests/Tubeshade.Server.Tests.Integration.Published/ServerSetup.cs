using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published;

[SetUpFixture]
public static class ServerSetup
{
    internal static KeycloakFixture Keycloak { get; } = new("26.8.1-1");

    internal static IServerFixture[] Fixtures { get; } =
    [
        new ServerFixture("Debug with coverage", "tubeshade-cover-tests", "18.6-trixie", Keycloak, true),
        new ServerFixture("Release", "tubeshade-integration-tests", "18.6-trixie", Keycloak, false),
    ];

    internal static BrowserFixture Browser { get; } = new();

    [OneTimeSetUp]
    public static Task OneTimeSetUpAsync()
    {
        Keycloak.Start();

        return Task.WhenAll(Fixtures
            .Select(fixture => fixture.InitializeAsync())
            .Append(Browser.InitializeAsync()));
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDownAsync()
    {
        await Task.WhenAll(Fixtures
            .Select(fixture => fixture.DisposeAsync().AsTask())
            .Append(Browser.DisposeAsync().AsTask()));

        await Keycloak.DisposeAsync();
    }
}
