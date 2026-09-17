using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;
using VerifyNUnit;

namespace Tubeshade.Server.Tests.Integration.Published.Pages.Playlists;

public sealed class CreateTests(IServerFixture serverFixture) : PlaywrightTests(serverFixture)
{
    /// <inheritdoc />
    protected override string Username { get; } = $"{Guid.NewGuid():N}@example.org";

    [Test]
    public async Task Index()
    {
        await Page.GotoAsync("/");
        await Page.GetByText("Creators").ClickAsync();
        await Page.GetByText("+").ClickAsync();

        const string libraryName = "Foolib";

        await CreateLibrary(libraryName);

        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Playlists" }).ClickAsync();

        var snapshot = await Page.Locator("body").AriaSnapshotAsync();
        await Verifier
            .Verify(snapshot)
            .ScrubGuids()
            .ScrubInlineGuids()
            .UseParameters("Any")
            .DisableRequireUniquePrefix();

        await Page.GetByLabel("Sort by").SelectOptionAsync("Videos");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Libraries" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = libraryName }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Playlists" }).ClickAsync();
    }

    private async Task CreateLibrary(string name)
    {
        await Page.GotoAsync("/Libraries");
        (await Page.TitleAsync()).Should().Be("Libraries - Tubeshade");

        await Page.GetByText("Libraries").ClickAsync();
        if (await Page.GetByRole(AriaRole.Link).GetByText(name).IsHiddenAsync())
        {
            await Page.GetByLabel("Name").FillAsync(name);
            await Page.GetByLabel("Storage path").FillAsync(ServerFixture.TestDirectory);

            await Page.GetByRole(AriaRole.Button, new() { Name = "Create library" }).ClickAsync();
            (await Page.TitleAsync()).Should().Be($"{name} - Tubeshade");
        }
    }
}
