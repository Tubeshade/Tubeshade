using System;
using AwesomeAssertions;
using NUnit.Framework;
using Tubeshade.Server.Services;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Tests.Services;

public sealed class YtdlpErrorOutputTests
{
    private const string InvalidCookies =
        "WARNING: [youtube] The provided YouTube account cookies are no longer valid. They have likely been rotated in the browser as a security measure.";

    private const string SignInRequired =
        "WARNING: [youtube] Sign in to confirm you’re not a bot. This helps protect our community. Use --cookies-from-browser or --cookies for the authentication.";

    private const string NoFormats = "WARNING: No video formats found!";

    [Test]
    public void ThrowIfSignInRequired_ShouldThrow_WhenCookiesAreInvalid()
    {
        var action = () => YtdlpErrorOutput.ThrowIfSignInRequired([InvalidCookies, SignInRequired, NoFormats], Data());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cookies for this library are no longer valid*");
    }

    [Test]
    public void ThrowIfSignInRequired_ShouldThrow_WithoutCookies()
    {
        var action = () => YtdlpErrorOutput.ThrowIfSignInRequired([SignInRequired, null, NoFormats], null);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Provide valid YouTube cookies*");
    }

    [Test]
    public void ThrowIfSignInRequired_ShouldNotThrow_WhenFormatsWereFound()
    {
        var data = Data();
        data.Formats = [new() { FormatId = "18", Format = "18", Extension = "mp4", Url = "https://example.com" }];

        var action = () => YtdlpErrorOutput.ThrowIfSignInRequired([InvalidCookies, SignInRequired], data);

        action.Should().NotThrow();
    }

    [Test]
    public void ThrowIfSignInRequired_ShouldNotThrow_WithoutSignInCheck()
    {
        var action = () => YtdlpErrorOutput.ThrowIfSignInRequired([InvalidCookies, NoFormats], Data());

        action.Should().NotThrow();
    }

    private static VideoData Data() => new() { Id = "rymDzMIGn5Y", Title = "Test video" };
}