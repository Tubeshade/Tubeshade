using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace PubSubHubbub;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SubscriptionMode : SmartEnum<SubscriptionMode>
{
    public static readonly SubscriptionMode Subscribe = new("subscribe", 1);
    public static readonly SubscriptionMode Unsubscribe = new("unsubscribe", 2);

    private SubscriptionMode(string name, int value)
        : base(name, value)
    {
    }
}
