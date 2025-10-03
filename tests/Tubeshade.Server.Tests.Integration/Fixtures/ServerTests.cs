using NUnit.Framework;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
public abstract class ServerTests
{
    protected ServerFixture Fixture { get; }

    protected ServerTests(ServerFixture fixture)
    {
        Fixture = fixture;
    }
}
