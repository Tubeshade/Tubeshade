using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using PubSubHubbub;
using Tubeshade.Server.Tests.Integration.Published.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Published.V1.Controllers.YouTube;

public sealed class NotificationsControllerTests(IServerFixture fixture) : PlaywrightTests(fixture)
{
    /// <inheritdoc />
    protected override bool LogIn => false;

    [Test]
    public async Task Get_ShouldReturnExpected()
    {
        var query = string.Join('&', new[]
        {
            $"hub.mode={SubscriptionMode.Subscribe.Name}",
            $"hub.topic={Fixture.BaseAddress.AbsoluteUri}",
            $"hub.challenge={Guid.NewGuid()}",
        });

        using var client = Fixture.HttpClient;
        using var response = await client.GetAsync($"api/v1.0/YouTube/Notifications/{Guid.NewGuid()}?{query}");
        var content = await response.Content.ReadAsStringAsync();

        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        content.Should().Contain("Channel does not exist");
    }

    [Test]
    public async Task Post_ShouldReturnExpected()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1.0/YouTube/Notifications/{Guid.NewGuid()}")
        {
            Content = new StringContent(
                // lang=xml
                """
                <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015"
                         xmlns="http://www.w3.org/2005/Atom">
                  <link rel="hub" href="https://pubsubhubbub.appspot.com"/>
                  <link rel="self" href="https://www.youtube.com/xml/feeds/videos.xml?channel_id=CHANNEL_ID"/>
                  <title>YouTube video feed</title>
                  <updated>2015-04-01T19:05:24.552394234+00:00</updated>
                  <entry>
                    <id>yt:video:VIDEO_ID</id>
                    <yt:videoId>VIDEO_ID</yt:videoId>
                    <yt:channelId>CHANNEL_ID</yt:channelId>
                    <title>Video title</title>
                    <link rel="alternate" href="http://www.youtube.com/watch?v=VIDEO_ID"/>
                    <author>
                     <name>Channel title</name>
                     <uri>http://www.youtube.com/channel/CHANNEL_ID</uri>
                    </author>
                    <published>2015-03-06T21:40:57+00:00</published>
                    <updated>2015-03-09T19:05:24.552394234+00:00</updated>
                  </entry>
                </feed>
                """,
                Encoding.UTF8,
                MediaTypeNames.Application.Xml)
        };

        using var client = Fixture.HttpClient;
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        using var scope = new AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        content.Should().Contain("Channel does not exist");
    }
}
