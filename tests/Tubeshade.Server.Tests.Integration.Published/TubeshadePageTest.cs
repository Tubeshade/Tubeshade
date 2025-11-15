using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using TUnit.Playwright;

namespace Tubeshade.Server.Tests.Integration.Published;

public abstract class TubeshadePageTest : PageTest
{
    private const string Username = "test@example.org";
    private static readonly string Password = Guid.NewGuid().ToString("N");

    private static readonly WebApplicationFactory Factory = new();

    /// <inheritdoc />
    public sealed override string BrowserName => Microsoft.Playwright.BrowserType.Firefox;

    /// <inheritdoc />
    public sealed override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        return new(base.ContextOptions(testContext))
        {
            BaseURL = Factory.GetBaseAddress().AbsoluteUri,
        };
    }

    [Before(TestSession)]
    public static async Task SetUp()
    {
        await Factory.InitializeAsync();

        var browser = await Playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions());
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{Factory.GetBaseAddress()}Identity/Account/Register");
        await Assert.That(page.TitleAsync()).IsEqualTo("Register - Tubeshade");

        await page.GetByLabel("Username").FillAsync(Username);
        await page.GetByLabel("Password", new PageGetByLabelOptions { Exact = true }).FillAsync(Password);
        await page.GetByLabel("Confirm Password").FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Register" }).ClickAsync();

        await Assert.That(page.TitleAsync()).IsEqualTo("Home page - Tubeshade");
    }

    [After(TestSession)]
    public static async Task TearDown()
    {
        await Factory.DisposeAsync();
    }

    [Before(Test)]
    public async Task TestLogin() => await Login();

    protected async Task Login()
    {
        await Page.GotoAsync("/Identity/Account/Login");

        await Assert.That(Page.TitleAsync()).IsEqualTo("Log in - Tubeshade");

        await Page.GetByLabel("Username").FillAsync(Username);
        await Page.GetByLabel("Password").FillAsync(Password);
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();

        await Assert.That(Page.TitleAsync()).IsEqualTo("Home page - Tubeshade");
    }
}
