using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Net.Http.Headers;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Tubeshade.Server.Services.Background;
using Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

[TestFixtureSource(typeof(ServerFixtureSource))]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public abstract class PlaywrightTests
{
    public const string DefaultCulture = "en-US";

    private const int DebugPort = 6005;
    private static int _debugPortOffset;

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
        var debugPort = DebugPort + Interlocked.Increment(ref _debugPortOffset);

        var browser = await Playwright[_browserType].LaunchAsync(new BrowserTypeLaunchOptions
        {
            Args = ["--start-debugger-server", $"{debugPort}"],
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["devtools.debugger.remote-enabled"] = true,
                ["devtools.debugger.prompt-connection"] = false,
                ["extensions.manifestV3.enabled"] = false,
            },
        });

        _browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseAddress.AbsoluteUri,
            ColorScheme = ColorScheme.Dark,
            Locale = Culture,
            ExtraHTTPHeaders = [new(HeaderNames.AcceptLanguage, Culture)],
        });

        using (var firefoxClient = new FirefoxDebuggerClient(debugPort))
        {
            var extensionPath = GetRelativePath("../../../extensions/browser/extension");
            if (!Directory.Exists(extensionPath))
            {
                throw new InvalidOperationException($"Could not resolve correct extension directory {extensionPath}");
            }

            await firefoxClient.Connect();
            await firefoxClient.InstallExtension(extensionPath);
        }

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

        if (_browserContext is not null)
        {
            await _browserContext.CloseAsync();
        }
    }

    private static string GetRelativePath(string relativePath, [CallerFilePath] string filePath = "")
    {
        var directory = Path.GetDirectoryName(filePath)!;
        return Path.GetFullPath(relativePath, directory);
    }
}
