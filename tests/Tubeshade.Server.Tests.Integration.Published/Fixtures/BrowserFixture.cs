using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Tubeshade.Server.Tests.Integration.Published.Fixtures.Firefox;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

/// <summary>A single Firefox instance with the browser extension installed, shared by all tests.</summary>
public sealed class BrowserFixture : IAsyncDisposable
{
    private const int DebugPort = 6005;

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private IBrowser Browser => _browser ?? throw new InvalidOperationException();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Args = ["--start-debugger-server", $"{DebugPort}"],
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["devtools.debugger.remote-enabled"] = true,
                ["devtools.debugger.prompt-connection"] = false,
                ["extensions.manifestV3.enabled"] = false,
            },
        });

        using var firefoxClient = new FirefoxDebuggerClient(DebugPort);

        var extensionPath = PathHelper.GetRelativePath("../../../extensions/browser/extension");
        if (!Directory.Exists(extensionPath))
        {
            throw new InvalidOperationException($"Could not resolve correct extension directory {extensionPath}");
        }

        await firefoxClient.Connect(cancellationToken);
        await firefoxClient.InstallExtension(extensionPath, cancellationToken);
    }

    public Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options) => Browser.NewContextAsync(options);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
