using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;
using VerifyNUnit;

namespace Tubeshade.Server.Tests.Integration.Published.Pages.Creators;

[Parallelizable(ParallelScope.None)] // Creating channels in parallel causes serialization issues in DB
public sealed class CreateTests(IServerFixture serverFixture) : PlaywrightTests(serverFixture)
{
    /// <inheritdoc />
    protected override string Username { get; } = $"{Guid.NewGuid():N}@example.org";

    [Test]
    public async Task CreateAndUpdate()
    {
        await Page.GotoAsync("/");
        await Page.GetByText("Creators").ClickAsync();
        await Page.GetByText("+").ClickAsync();

        const string creatorName = "Foo";
        const string libraryName = "Foolib";
        var channelNames = Enumerable.Range(1, 5).Select(index => $"Channel {index}").ToArray();

        await CreateLibrary(libraryName);
        foreach (var channelName in channelNames)
        {
            await CreateChannel(channelName, libraryName);
        }

        await Page.GotoAsync("/");
        await Page.GetByText("Creators").ClickAsync();

        await Page.GetByText("+").ClickAsync();
        await Page.GetByLabel("Name").FillAsync(creatorName);
        await Page.GetByLabel("Primary channel").SelectOptionAsync(channelNames.First());
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create creator" }).ClickAsync();

        (await Page.TitleAsync()).Should().Be($"{creatorName} - Tubeshade");

        await Page.GetByText("✎").ClickAsync();
        await Page.GetByLabel("Channels").SelectOptionAsync(channelNames.Skip(3));
        await Page.GetByText("Update creator").ClickAsync();

        var snapshot = await Page.Locator("body").AriaSnapshotAsync();
        await Verifier
            .Verify(snapshot)
            .ScrubGuids()
            .ScrubInlineGuids()
            .UseParameters("Any")
            .DisableRequireUniquePrefix();

        await Page.GetByText("Creators").ClickAsync();
        await Page.GetByLabel("Sort by").SelectOptionAsync("Channels");
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

    private async Task CreateChannel(string name, string libraryName)
    {
        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be("Channels - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "+" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Create a channel - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(name);
        await Page.GetByLabel("Original ID").FillAsync(name);
        await Page.GetByLabel("Original URL").FillAsync(name);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{name} - {libraryName} - Tubeshade");
    }
}
