using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.Pages.Libraries;

public sealed class IndexTests(IServerFixture serverFixture) : PlaywrightTests(serverFixture)
{
    [Test]
    public async Task CreateLibrary()
    {
        const string name = "Foo";

        await Page.GotoAsync("/Libraries");
        (await Page.TitleAsync()).Should().Be("Libraries - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(name);
        await Page.GetByLabel("Storage path").FillAsync(ServerFixture.TestDirectory);

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create library" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{name} - Tubeshade");

        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByText("Libraries").ClickAsync();
        await Page.GetByText(name).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{name} - Tubeshade");

        var pages = new KeyValuePair<string, string>[]
        {
            new("Channels", $"Channels - {name} - Tubeshade"),
            new("Downloads", $"Downloads - {name} - Tubeshade"),
            new("Tasks", $"Tasks - {name} - Tubeshade"),
            new("Settings", $"Preferences - {name} - Tubeshade"),
        };

        foreach (var (navigationLink, title) in pages)
        {
            await Page.GetByText(navigationLink).ClickAsync();

            (await Page.TitleAsync()).Should().Be(title);

            await Page.GoBackAsync();
        }
    }
}
