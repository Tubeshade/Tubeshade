using System;
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

        await Page.GetByText("Libraries").ClickAsync();
        if (await Page.GetByRole(AriaRole.Link).GetByText(name).IsHiddenAsync())
        {
            await Page.GetByLabel("Name").FillAsync(name);
            await Page.GetByLabel("Storage path").FillAsync(ServerFixture.TestDirectory);

            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create library" }).ClickAsync();
            (await Page.TitleAsync()).Should().Be($"{name} - Tubeshade");
        }

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

        await Page.GetByText("Downloads").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Downloads - {name} - Tubeshade");

        await Page.GetByPlaceholder("Video or channel URL").FillAsync("https://www.youtube.com/@Computerphile");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scan" }).ClickAsync();

        await Page.GetByText("Tasks").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Tasks - {name} - Tubeshade");

        await Page.GetByText("Completed").WaitForAsync();

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Channels - {name} - Tubeshade");

        await Page.GetByText("Computerphile").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Computerphile - {name} - Tubeshade");

        await Page.GetByText("Settings").ClickAsync();
        (await Page.TitleAsync()).Should().Be("Preferences - Computerphile - Tubeshade");

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Subscribe" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Unsubscribe" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Subscribe" }).ClickAsync();

        await Page.GetByText("Video count").FillAsync("1");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Update preferences" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scan", Exact = true }).ClickAsync();

        await Page.GetByText("Tasks").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Tasks - {name} - Tubeshade");

        await Page
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Page
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Retry" })
            .ClickAsync(new LocatorClickOptions { Timeout = 5_000 });

        await Page
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scan sponsor segments" })
            .ClickAsync(new LocatorClickOptions { Timeout = 5_000 });

        await Page
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Update sponsor segments" })
            .ClickAsync(new LocatorClickOptions { Timeout = 5_000 });

        await Page
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Channels - {name} - Tubeshade");
        await Page.GetByText(DateTimeOffset.Now.ToString("yyyy-MM-dd")).WaitForAsync();
    }
}
