using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration;

public sealed class HealthChecksTests(ServerFixture fixture) : ServerTests(fixture)
{
    [Test]
    public async Task ShouldBeHealthy()
    {
        using var client = Fixture.CreateHttpClient();
        using var response = await client.GetAsync("/healthz");
        var content = await response.Content.ReadAsStringAsync();

        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Healthy");
    }
}
