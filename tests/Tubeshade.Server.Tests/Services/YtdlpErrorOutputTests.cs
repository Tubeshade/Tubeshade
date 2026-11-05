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

    [Test]
    public void GetErrorMessage_MembersOnly()
    {
        string?[] output =
        [
            "[debug] [youtube] K8IFr6RlN-I: web_embedded player response playability status: UNPLAYABLE",
            "WARNING: [youtube] This video is available to this channel's members on level: Super Special Fiend (or any higher level). Join this channel to get access to members-only content and other exclusive perks.",
            NoFormats,
            "WARNING: Requested format is not available"
        ];

        YtdlpErrorOutput.GetErrorMessage(output).Should().Be(
            "This video is only available to members of the channel. The YouTube account in the cookies for this library needs to be a member on at least the \"Super Special Fiend\" level to download it.");
    }

    [Test]
    public void GetErrorMessage_MembersOnlyWithoutLevel()
    {
        string?[] output =
            ["WARNING: [youtube] This video is available to this channel’s members. Join this channel to get access."];

        YtdlpErrorOutput.GetErrorMessage(output).Should().Be(
            "This video is only available to members of the channel. The YouTube account in the cookies for this library needs to be a member to download it.");
    }

    [Test]
    public void GetErrorMessage_SignInRequired()
    {
        YtdlpErrorOutput.GetErrorMessage([InvalidCookies, SignInRequired, NoFormats])
            .Should().Contain("cookies for this library are no longer valid");
    }

    [Test]
    public void GetErrorMessage_ShouldOnlyIncludeWarningsAndErrors()
    {
        string?[] output =
        [
            "[debug] Command-line config: ['-J', '-v']",
            null,
            "WARNING: No video formats found!\nERROR: [youtube] abc: Video unavailable",
            "[debug] Default format spec: bestvideo*+bestaudio/best",
            "WARNING: No video formats found!"
        ];

        YtdlpErrorOutput.GetErrorMessage(output).Should().Be(
            $"WARNING: No video formats found!{Environment.NewLine}ERROR: [youtube] abc: Video unavailable");
    }

    [Test]
    public void GetErrorMessage_ShouldFallBackToFullOutput()
    {
        YtdlpErrorOutput.GetErrorMessage(["something went wrong", " "]).Should().Be("something went wrong");
    }

    private static VideoData Data() => new() { Id = "rymDzMIGn5Y", Title = "Test video" };
}
