using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PubSubHubbub;

public sealed class SubscriptionRequest
{
    /// <summary>Gets the subscriber's callback URL where notifications should be delivered.</summary>
    /// <remarks>It is considered good practice to use a unique callback URL for each subscription.</remarks>
    public required Uri Callback { get; init; }

    /// <summary>Gets the goal of the request.</summary>
    public required SubscriptionMode Mode { get; init; }

    /// <summary>Gets the topic URL that the subscriber wishes to subscribe to or unsubscribe from.</summary>
    public required Uri Topic { get; init; }

    /// <summary>Gets the time for which the subscriber would like to have the subscription active.</summary>
    /// <remarks>
    /// Hubs MAY choose to respect this value or not, depending on their own policies.
    /// This parameter MAY be present for unsubscription requests and MUST be ignored by the hub in that case.
    /// </remarks>
    public TimeSpan? Lease { get; init; }

    /// <summary>Gets a subscriber-provided secret string that will be used to compute an HMAC digest for authorized content distribution [authednotify].</summary>
    /// <remarks>
    /// If not supplied, the HMAC digest will not be present for content distribution requests.
    /// This parameter SHOULD only be specified when the request was made over HTTPS [RFC2818].
    /// This parameter MUST be less than 200 bytes in length.
    /// </remarks>
    public string? Secret { get; init; }

    public string? VerifyToken { get; init; }

    public FormUrlEncodedContent ToContent()
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("hub.callback", Callback.ToString()),
            new("hub.mode", Mode.Name),
            new("hub.topic", Topic.ToString()),
            new("hub.verify", "sync"),
        };

        if (Secret is not null)
        {
            values.Add(new("hub.secret", Secret));
        }

        if (VerifyToken is not null)
        {
            values.Add(new("hub.verify_token", VerifyToken));
        }

        return new(values);
    }
}
