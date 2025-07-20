using System;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services.Background;

public sealed class ScanSubscriptionsBackgroundService
    : TaskBackgroundServiceBase<YoutubeService, ScanSubscriptionsPayload>
{
    /// <inheritdoc />
    public ScanSubscriptionsBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.ScanSubscriptions, TaskPayloadContext.Default.ScanSubscriptionsPayload)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, ScanSubscriptionsPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.ScanSubscriptions(
            context.Payload.LibraryId,
            context.Payload.UserId,
            context.TaskRepository,
            context.TaskRunId,
            context.Directory,
            cancellationToken);
    }
}
