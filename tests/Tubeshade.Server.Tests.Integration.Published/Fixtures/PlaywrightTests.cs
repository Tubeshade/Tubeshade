using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
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

    protected static readonly string Password = Guid.NewGuid().ToString("N");
    private static readonly ConcurrentDictionary<IServerFixture, SemaphoreSlim> FixtureLocks = new();
    private static readonly ConcurrentDictionary<(IServerFixture, string), bool> Registered = new();
    private static readonly ConcurrentDictionary<(IServerFixture, string), string> StorageStates = new();

    private readonly string _browserType;
    private readonly string? _device;

    private IBrowserContext? _browserContext;
    private IPage? _page;

    protected string Culture { get; }

    protected virtual bool LogIn => true;

    protected virtual string Username => "test@example.org";

    protected virtual bool Trace => false;

    protected IServerFixture Fixture { get; }

    protected IBrowserContext Context => _browserContext ?? throw new InvalidOperationException();

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

    [SetUp]
    public async Task SetUp()
    {
        var options = new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.AbsoluteUri,
            ColorScheme = ColorScheme.Dark,
            Locale = Culture,
            ExtraHTTPHeaders = [new(HeaderNames.AcceptLanguage, Culture)],
        };

        if (LogIn)
        {
            options.StorageState = await GetStorageState(options);
        }

        _browserContext = await ServerSetup.Browser.NewContextAsync(options);
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

        if (_browserContext is not null)
        {
            await _browserContext.CloseAsync();
        }
    }

    protected async Task UsernamePasswordLogin()
    {
        using (await LockFixture())
        {
            await Register(Page);
        }

        await Login(Page);
    }

    /// <summary>Signs in once per server and user, so that other tests can start already signed in.</summary>
    private async Task<string> GetStorageState(BrowserNewContextOptions options)
    {
        using (await LockFixture())
        {
            if (StorageStates.TryGetValue((Fixture, Username), out var storageState))
            {
                return storageState;
            }

            await using var context = await ServerSetup.Browser.NewContextAsync(options);
            var page = await context.NewPageAsync();

            // Registering also signs in
            if (!await Register(page))
            {
                await Login(page);
            }

            storageState = await context.StorageStateAsync();
            StorageStates[(Fixture, Username)] = storageState;
            return storageState;
        }
    }

    // Must be called while holding the fixture lock
    private async Task<bool> Register(IPage page)
    {
        if (Registered.TryGetValue((Fixture, Username), out var registered) && registered)
        {
            return false;
        }

        await page.GotoAsync("/Identity/Account/Register");
        if (Culture is DefaultCulture)
        {
            (await page.TitleAsync()).Should().Be("Register - Tubeshade");
        }

        await page.GetByLabel("Username").FillAsync(Username);
        await page.GetByLabel("Password", new PageGetByLabelOptions { Exact = true }).FillAsync(Password);
        await page.GetByLabel("Confirm Password").FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Register" }).ClickAsync();

        if (Culture is DefaultCulture)
        {
            (await page.TitleAsync()).Should().Be("Home page - Tubeshade");
        }

        Registered[(Fixture, Username)] = true;
        return true;
    }

    private async Task Login(IPage page)
    {
        await page.GotoAsync("/Identity/Account/Login");

        if (Culture is DefaultCulture)
        {
            (await page.TitleAsync()).Should().Be("Log in - Tubeshade");
        }

        await page.GetByLabel("Username").FillAsync(Username);
        await page.GetByLabel("Password").FillAsync(Password);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Log in" }).ClickAsync();

        if (Culture is DefaultCulture)
        {
            (await page.TitleAsync()).Should().Be("Home page - Tubeshade");
        }
    }

    protected async Task<SemaphoreSlimExtensions.SemaphoreScope> LockFixture()
    {
        return await FixtureLocks[Fixture].LockAsync();
    }
}
