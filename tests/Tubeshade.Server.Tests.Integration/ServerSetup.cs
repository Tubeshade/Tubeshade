using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration;

[SetUpFixture]
public static class ServerSetup
{
    internal static ServerFixture[] Fixtures { get; } =
    [
        new("17.5-bookworm"),
    ];

    [OneTimeSetUp]
    public static Task OneTimeSetUpAsync()
    {
        return Task.WhenAll(Fixtures.Select(fixture => fixture.InitializeAsync().AsTask()));
    }

    [OneTimeTearDown]
    public static Task OneTimeTearDownAsync()
    {
        return Task.WhenAll(Fixtures.Select(fixture => fixture.DisposeAsync().AsTask()));
    }
}
