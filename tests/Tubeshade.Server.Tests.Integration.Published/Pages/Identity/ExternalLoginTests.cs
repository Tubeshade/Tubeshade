using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.Pages.Identity;

[Parallelizable(ParallelScope.Fixtures)]
public sealed class ExternalLoginTests(IServerFixture fixture) : PlaywrightTests(fixture)
{
    /// <inheritdoc />
    protected override bool LogIn => false;

    [Test]
    public async Task AddExternalLogin()
    {
        await UsernamePasswordLogin();

        await NavigateToExternalLoginManagement();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Keycloak" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Sign in to Test");

        await KeycloakLogin();

        (await Page.TitleAsync()).Should().Be("Manage your external logins - Tubeshade");
        await Page.GetByText("The external login was added.").IsVisibleAsync();

        await RemoveKeycloakLogin();
    }

    [Test]
    public async Task RegisterWithExternal()
    {
        await Page.GotoAsync("/");

        (await Page.TitleAsync()).Should().Be("Log in - Tubeshade");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Keycloak" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Sign in to Test");

        await KeycloakLogin();

        (await Page.TitleAsync()).Should().Be("Register - Tubeshade");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Account" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Profile - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Password" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Set password - Tubeshade");

        await Page.GetByLabel("New password", new() { Exact = true }).FillAsync(Password);
        await Page.GetByLabel("Confirm new password").FillAsync(Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Set password" }).ClickAsync();

        await NavigateToExternalLoginManagement();
        await RemoveKeycloakLogin();
    }

    private async Task KeycloakLogin()
    {
        await Page.GetByLabel("Username or email").FillAsync("john.doe");
        await Page.GetByLabel("Password", new() { Exact = true }).FillAsync("1qaz2wsx");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();

        await Task.Delay(1_000);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task NavigateToExternalLoginManagement()
    {
        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Account" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Profile - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "External logins" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Manage your external logins - Tubeshade");
    }

    private async Task RemoveKeycloakLogin()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Keycloak" }).IsVisibleAsync();
    }
}
