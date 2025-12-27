using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.Pages.Channels;

[Parallelizable(ParallelScope.None)] // Creating channels in parallel causes serialization issues in DB
public sealed class CreateTests(IServerFixture serverFixture) : PlaywrightTests(serverFixture)
{
    private const string Name = "Foo";

    /// <inheritdoc />
    protected override string Username => "test3@example.org";

    [Test]
    [Order(1)]
    public async Task CreateChannelFromLibrary()
    {
        await Page.GotoAsync("/Libraries");
        (await Page.TitleAsync()).Should().Be("Libraries - Tubeshade");

        await Page.GetByText("Libraries").ClickAsync();
        if (await Page.GetByRole(AriaRole.Link).GetByText(Name).IsHiddenAsync())
        {
            await Page.GetByLabel("Name").FillAsync(Name);
            await Page.GetByLabel("Storage path").FillAsync(ServerFixture.TestDirectory);

            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create library" }).ClickAsync();
            (await Page.TitleAsync()).Should().Be($"{Name} - Tubeshade");
        }

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Channels - {Name} - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"Create a channel - {Name} - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(Name);
        await Page.GetByLabel("Original ID").FillAsync(Name);
        await Page.GetByLabel("Original URL").FillAsync(Name);

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{Name} - {Name} - Tubeshade");
    }

    [Test]
    [Order(2)]
    public async Task CreateChannel()
    {
        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be("Channels - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Create a channel - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(Name);
        await Page.GetByLabel("Original ID").FillAsync(Name);
        await Page.GetByLabel("Original URL").FillAsync(Name);

        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{Name} - {Name} - Tubeshade");
    }
}
