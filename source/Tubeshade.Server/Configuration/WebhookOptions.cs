namespace Tubeshade.Server.Configuration;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public bool CheckForPosts { get; set; } = true;
}
