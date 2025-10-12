using NodaTime;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed record ChannelSubscriptionEntity : ModifiableEntity
{
    public required SubscriptionStatus Status { get; set; }

    public required string Callback { get; set; }

    public required string Topic { get; set; }

    public Instant? ExpiresAt { get; set; }

    public string? VerifyToken { get; set; }

    public string? Secret { get; set; }
}
