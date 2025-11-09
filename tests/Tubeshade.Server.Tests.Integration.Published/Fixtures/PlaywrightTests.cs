using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
[Parallelizable(ParallelScope.None)]
public abstract class PlaywrightTests
{
    private const string Username = "test@example.org";
    private static readonly string Password = Guid.NewGuid().ToString("N");
    private static readonly SemaphoreSlim RegistrationLock = new(1);
    private static readonly ConcurrentDictionary<IServerFixture, bool> Registered = new();

    private static IPlaywright? _playwright;

    private static IPlaywright Playwright => _playwright ?? throw new InvalidOperationException();

    private readonly string _browserType;
    private readonly string? _device;

    private IBrowserContext? _browserContext;
    private IPage? _page;

    protected virtual bool LogIn => true;

    protected virtual bool Trace => false;

    protected IServerFixture Fixture { get; }

    protected IPage Page => _page ?? throw new InvalidOperationException();

    private string TraceName => string.Join(
        '_',
        new[]
        {
            TestContext.CurrentContext.Test.ClassName,
            Fixture.Name.Replace(' ', '_'),
            _browserType,
            _device,
            TestContext.CurrentContext.Test.Name
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

    protected PlaywrightTests(IServerFixture serverFixture)
    {
        Fixture = serverFixture;
        _browserType = BrowserType.Firefox;
        _device = null;

        Registered.TryAdd(Fixture, false);
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        using var lockScope = await RegistrationLock.LockAsync();

        if (!LogIn || Registered[Fixture])
        {
            return;
        }

        var browser = await Playwright[_browserType].LaunchAsync();
        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.AbsoluteUri,
            ColorScheme = ColorScheme.Dark,
            Locale = "en-US",
        });

        var page = await browserContext.NewPageAsync();

        await page.GotoAsync("/Identity/Account/Register");
        (await page.TitleAsync()).Should().Be("Register - Tubeshade");

        await page.GetByLabel("Username").FillAsync(Username);
        await page.GetByLabel("Password", new PageGetByLabelOptions { Exact = true }).FillAsync(Password);
        await page.GetByLabel("Confirm Password").FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Register" }).ClickAsync();

        (await page.TitleAsync()).Should().Be("Home page - Tubeshade");

        Registered[Fixture] = true;
    }

    [OneTimeTearDown]
    public Task OneTimeTearDown()
    {
        _playwright?.Dispose();
        return Task.CompletedTask;
    }

    [SetUp]
    public async Task SetUp()
    {
        var browser = await Playwright[_browserType].LaunchAsync();
        _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.AbsoluteUri,
            ColorScheme = ColorScheme.Dark,
            Locale = "en-US",
        });

        _page = await _browserContext.NewPageAsync();

        if (Trace)
        {
            await _browserContext.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
                Title = TraceName,
            });
        }

        if (LogIn)
        {
            await Page.GotoAsync("/Identity/Account/Login");

            (await Page.TitleAsync()).Should().Be("Log in - Tubeshade");

            await Page.GetByLabel("Username").FillAsync(Username);
            await Page.GetByLabel("Password").FillAsync(Password);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();

            (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        var elements = await Page.QuerySelectorAllAsync(".field-validation-error");
        foreach (var element in elements)
        {
            var innerText = await element.InnerTextAsync();
            if (!string.IsNullOrWhiteSpace(innerText))
            {
                await TestContext.Out.WriteLineAsync(innerText);
            }
        }

        if (Trace && _browserContext is not null)
        {
            await _browserContext.Tracing.StopAsync(new()
            {
                Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces", $"{TraceName}.zip"),
            });
        }
    }
}
