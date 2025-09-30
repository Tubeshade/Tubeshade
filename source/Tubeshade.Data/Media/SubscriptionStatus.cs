using Ardalis.SmartEnum;

namespace Tubeshade.Data.Media;

public sealed class SubscriptionStatus : SmartEnum<SubscriptionStatus, int>
{
    public static readonly SubscriptionStatus SubscriptionPending = new("subscription_pending", 1);
    public static readonly SubscriptionStatus Subscribed = new("subscribed", 2);
    public static readonly SubscriptionStatus UnsubscriptionPending = new("unsubscription_pending", 3);

    private SubscriptionStatus(string name, int value)
        : base(name, value)
    {
    }
}
