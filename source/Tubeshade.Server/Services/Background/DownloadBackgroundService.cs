using System;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services.Background;

public sealed class DownloadBackgroundService : TaskBackgroundServiceBase<YoutubeService, DownloadVideoPayload>
{
    /// <inheritdoc />
    public DownloadBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.DownloadVideo, TaskPayloadContext.Default.DownloadVideoPayload)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, DownloadVideoPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.DownloadVideo(
            context.Payload.LibraryId,
            context.Payload.VideoId,
            context.Payload.UserId,
            context.TaskRepository,
            context.TaskRunId,
            context.Directory,
            cancellationToken);
    }
}
