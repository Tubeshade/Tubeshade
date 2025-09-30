using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PubSubHubbub;

public sealed class PubSubHubbubClient
{
    private readonly HttpClient _httpClient;

    public PubSubHubbubClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async ValueTask Subscribe(Uri callback, Uri topic, string? secret, string? verifyToken)
    {
        var content = new SubscriptionRequest
        {
            Callback = callback,
            Mode = SubscriptionMode.Subscribe,
            Topic = topic,
            Lease = null,
            Secret = secret,
            VerifyToken = verifyToken,
        }.ToContent();

        using var response = await _httpClient.PostAsync(new Uri("/subscribe", UriKind.Relative), content);
        response.EnsureSuccessStatusCode();
    }

    public async ValueTask Unsubscribe(Uri callback, Uri topic)
    {
        var content = new SubscriptionRequest
        {
            Callback = callback,
            Mode = SubscriptionMode.Unsubscribe,
            Topic = topic,
            Lease = null,
        }.ToContent();

        using var response = await _httpClient.PostAsync(new Uri("/subscribe", UriKind.Relative), content);
        response.EnsureSuccessStatusCode();
    }
}
