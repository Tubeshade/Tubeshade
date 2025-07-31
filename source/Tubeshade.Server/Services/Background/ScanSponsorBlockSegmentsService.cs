using System;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services.Background;

public sealed class ScanSponsorBlockSegmentsService : TaskBackgroundServiceBase<YoutubeService, ScanSponsorBlockSegmentsPayload>
{
    /// <inheritdoc />
    public ScanSponsorBlockSegmentsService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.ScanSponsorBlockSegments, TaskPayloadContext.Default.ScanSponsorBlockSegmentsPayload)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, ScanSponsorBlockSegmentsPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.ScanSponsorBlockSegments(
            context.Payload.LibraryId,
            context.Payload.UserId,
            context.TaskRepository,
            context.TaskRunId,
            context.Directory,
            cancellationToken);
    }
}
