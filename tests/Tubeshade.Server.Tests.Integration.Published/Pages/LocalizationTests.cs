using System.Threading.Tasks;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;
using VerifyNUnit;

namespace Tubeshade.Server.Tests.Integration.Published.Pages;

[TestFixtureSource(typeof(LocalizedServerFixtureSource))]
public sealed class LocalizationTests(IServerFixture serverFixture, string culture)
    : PlaywrightTests(serverFixture, culture)
{
    [TestCase("/")]
    [TestCase("/Downloads")]
    public async Task Snapshot(string url)
    {
        await Page.GotoAsync(url);
        var snapshot = await Page.Locator("body").AriaSnapshotAsync();

        await Verifier
            .Verify(snapshot)
            .UseParameters("Any", Culture, url)
            .DisableRequireUniquePrefix();
    }
}
