using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published;

[SetUpFixture]
public static class ServerSetup
{
    internal static IServerFixture[] Fixtures { get; } =
    [
        new ServerFixture("Debug with coverage", "tubeshade-cover-tests", "18.6-trixie", "26.8.1-1", true),
        new ServerFixture("Release", "tubeshade-integration-tests", "18.6-trixie", "26.8.1-1", false),
    ];

    [OneTimeSetUp]
    public static Task OneTimeSetUpAsync()
    {
        return Task.WhenAll(Fixtures.Select(fixture => fixture.InitializeAsync()));
    }

    [OneTimeTearDown]
    public static Task OneTimeTearDownAsync()
    {
        return Task.WhenAll(Fixtures.Select(fixture => fixture.DisposeAsync().AsTask()));
    }
}
