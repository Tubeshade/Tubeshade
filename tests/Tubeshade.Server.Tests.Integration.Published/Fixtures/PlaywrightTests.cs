using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Net.Http.Headers;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public abstract class PlaywrightTests
{
    public const string DefaultCulture = "en-US";

    private static readonly string Password = Guid.NewGuid().ToString("N");
    private static readonly SemaphoreSlim SetUpLock = new(1);
    private static readonly ConcurrentDictionary<IServerFixture, SemaphoreSlim> FixtureLocks = new();
    private static readonly ConcurrentDictionary<(IServerFixture, string), bool> Registered = new();

    private static IPlaywright? _playwright;

    private static IPlaywright Playwright => _playwright ?? throw new InvalidOperationException();

    private readonly string _browserType;
    private readonly string? _device;

    private IBrowserContext? _browserContext;
    private IPage? _page;

    protected string Culture { get; }

    protected virtual bool LogIn => true;

    protected virtual string Username => "test@example.org";

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
            Culture,
            TestContext.CurrentContext.Test.Name
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

    protected PlaywrightTests(IServerFixture serverFixture)
        : this(serverFixture, DefaultCulture)
    {
    }

    protected PlaywrightTests(IServerFixture serverFixture, string culture)
    {
        Culture = culture;
        Fixture = serverFixture;
        _browserType = BrowserType.Firefox;
        _device = null;

        FixtureLocks.TryAdd(serverFixture, new(1));
    }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        using (await SetUpLock.LockAsync())
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        var browser = await Playwright[_browserType].LaunchAsync();
        _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.AbsoluteUri,
            ColorScheme = ColorScheme.Dark,
            Locale = Culture,
            ExtraHTTPHeaders = [new(HeaderNames.AcceptLanguage, Culture)],
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

        if (!LogIn)
        {
            return;
        }

        using (await FixtureLocks[Fixture].LockAsync())
        {
            if (!Registered.TryGetValue((Fixture, Username), out var registered) || !registered)
            {
                await Page.GotoAsync("/Identity/Account/Register");
                if (Culture is DefaultCulture)
                {
                    (await Page.TitleAsync()).Should().Be("Register - Tubeshade");
                }

                await Page.GetByLabel("Username").FillAsync(Username);
                await Page.GetByLabel("Password", new PageGetByLabelOptions { Exact = true }).FillAsync(Password);
                await Page.GetByLabel("Confirm Password").FillAsync(Password);
                await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Register" }).ClickAsync();

                if (Culture is DefaultCulture)
                {
                    (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");
                }

                Registered[(Fixture, Username)] = true;
            }
        }

        await Page.GotoAsync("/Identity/Account/Login");

        if (Culture is DefaultCulture)
        {
            (await Page.TitleAsync()).Should().Be("Log in - Tubeshade");
        }

        await Page.GetByLabel("Username").FillAsync(Username);
        await Page.GetByLabel("Password").FillAsync(Password);
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();

        if (Culture is DefaultCulture)
        {
            (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status is TestStatus.Failed)
        {
            var snapshot = await Page.Locator("body").AriaSnapshotAsync();
            Console.WriteLine(snapshot);
        }

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
