using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;
using PubSubHubbub.Models;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.V1.Controllers.YouTube;

public sealed class WebhookTests(IServerFixture fixture) : PlaywrightTests(fixture)
{
    /// <inheritdoc />
    protected override string Username { get; } = $"{Guid.NewGuid():N}@example.org";

    [TestCaseSource(typeof(WebhookTestCaseSource))]
    public async Task Webhook(string videoUrlFormat, PreferencesEntity? preferences, VideoType? type, bool ignored)
    {
        Guid libraryId;
        Guid channelId;
        var name = Guid.NewGuid().ToString("N");

        using (await LockFixture())
        {
            libraryId = await CreateLibrary(name);
            channelId = await CreateChannel(libraryId, name, preferences);
        }

        var externalChannelId = Guid.NewGuid().ToString("N");
        var externalVideoId = Guid.NewGuid().ToString("N");
        var videoUrl = string.Format(videoUrlFormat, externalVideoId);

        if (type is not null)
        {
            _ = await CreateVideo(channelId, externalVideoId, videoUrl, type);
        }

        var feed = CreateRequest(externalChannelId, externalVideoId, videoUrl);
        await Notify(channelId, feed);

        await Page.GotoAsync($"/Libraries/{libraryId}/Tasks");

        await Page.GetByText($"Feed update from \"{name}\"").WaitForAsync();

        if (ignored)
        {
            return;
        }

        if (type is not null)
        {
        }
        else
        {
            await Page.GetByText($"Index \"{videoUrl}\"").WaitForAsync();
        }
    }

    private async Task<Guid> CreateLibrary(string name)
    {
        await Page.GotoAsync("/Libraries");
        (await Page.TitleAsync()).Should().Be("Libraries - Tubeshade");

        await Page.GetByText("Libraries").ClickAsync();
        await Page.GetByLabel("Name").FillAsync(name);
        await Page.GetByLabel("Storage path").FillAsync(ServerFixture.TestDirectory);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create library" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{name} - Tubeshade");

        var text = Page.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Guid.ParseExact(text, "D");
    }

    private async Task<Guid> CreateChannel(Guid libraryId, string name, PreferencesEntity? preferences)
    {
        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByText("Channels").ClickAsync();
        (await Page.TitleAsync()).Should().Be("Channels - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Create a channel - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(name);
        await Page.GetByLabel("Library").SelectOptionAsync(libraryId.ToString("D", CultureInfo.InvariantCulture));
        await Page.GetByLabel("Original ID").FillAsync(name);
        await Page.GetByLabel("Original URL").FillAsync(name);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create channel" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{name} - {name} - Tubeshade");

        var text = Page.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        var id = Guid.ParseExact(text, "D");
        if (preferences is null)
        {
            return id;
        }

        await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();

        await Page.GetByLabel("Video count").FillAsync(preferences.VideosCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        await Page.GetByLabel("Livestream count").FillAsync(preferences.LiveStreamsCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        await Page.GetByLabel("Short count").FillAsync(preferences.ShortsCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update preferences" }).ClickAsync();

        return id;
    }

    private async Task<Guid> CreateVideo(Guid channelId, string externalId, string externalUrl, VideoType type)
    {
        await Page.GotoAsync("/");
        (await Page.TitleAsync()).Should().Be("Home page - Tubeshade");

        await Page.GetByRole(AriaRole.Link, new() { Name = "+" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be("Create a video - Tubeshade");

        await Page.GetByLabel("Name").FillAsync(externalId);
        await Page.GetByLabel("Channel").SelectOptionAsync(channelId.ToString("D", CultureInfo.InvariantCulture));
        await Page.GetByLabel("Video type").SelectOptionAsync(type.Name);
        await Page.GetByLabel("Original ID").FillAsync(externalId);
        await Page.GetByLabel("Original URL").FillAsync(externalUrl);
        await Page.GetByLabel("Published at").FillAsync("2025-12-28T00:00");
        await Page.GetByLabel("Duration").FillAsync("10");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Create video" }).ClickAsync();
        (await Page.TitleAsync()).Should().Be($"{externalId} - Tubeshade");

        var text = Page.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        return Guid.ParseExact(text, "D");
    }

    private async Task Notify(Guid channelId, Feed feed)
    {
        var serializer = new XmlSerializer(typeof(Feed));
        await using var writer = new StringWriter();
        serializer.Serialize(writer, feed);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1.0/YouTube/Notifications/{channelId:D}");
        request.Content = new StringContent(writer.ToString(), Encoding.UTF8, MediaTypeNames.Application.Xml);

        using var client = Fixture.HttpClient;
        using var response = await client.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static Feed CreateRequest(string channelId, string videoId, string videoUrl) => new()
    {
        Links = [],
        Title = "YouTube video feed",
        Updated = default,
        Entry = new Entry
        {
            Id = $"yt:video:{videoId}",
            VideoId = videoId,
            ChannelId = channelId,
            Title = "Video title",
            Link = new() { Relation = "alternate", Uri = videoUrl },
            Author = new() { Name = "Channel title", Uri = $"https://www.youtube.com/channel/{channelId}" },
            Published = default,
            Updated = default,
        }
    };
}
