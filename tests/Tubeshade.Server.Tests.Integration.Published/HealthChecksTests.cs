using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published;

public sealed class HealthChecksTests(IServerFixture fixture) : PlaywrightTests(fixture)
{
    /// <inheritdoc />
    protected override bool LogIn => false;

    [Test]
    public async Task ShouldBeHealthy()
    {
        using var client = Fixture.HttpClient;
        using var response = await client.GetAsync("/healthz");
        var content = await response.Content.ReadAsStringAsync();

        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Healthy");
    }
}
