using Tubeshade.Data.AccessControl;

namespace Tubeshade.Server;

public static class SystemPrincipals
{
    public static readonly SystemPrincipal TaskRuns = new("Dequeuing task runs and tracking their state and progress");

    public static readonly SystemPrincipal Scheduler = new("Triggering scheduled tasks");

    public static readonly SystemPrincipal Cleanup = new("Deleting old tasks and finishing tasks stuck after a restart");

    public static readonly SystemPrincipal StartupMigrations = new("Queueing data migration tasks for every library at startup");

    public static readonly SystemPrincipal Webhook = new("Handling PubSubHubbub notifications for subscribed channels");

    public static readonly SystemPrincipal Subscriptions = new("Renewing expiring PubSubHubbub subscriptions");
}
