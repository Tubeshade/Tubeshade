using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration;

public sealed class YoutubeTests(ServerFixture fixture) : ServerTests(fixture)
{
    [Test]
    public void Test()
    {
        Assert.Pass();
    }
}
