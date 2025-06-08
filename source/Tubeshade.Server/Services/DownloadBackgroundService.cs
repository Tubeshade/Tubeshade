using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services;

public sealed class DownloadBackgroundService : ChannelConsumerBackgroundService<YoutubeService, DownloadVideoPayload>
{
    /// <inheritdoc />
    public DownloadBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskBackgroundService.DownloadTaskChannel.Reader, TaskType.DownloadVideo)
    {
    }

    /// <inheritdoc />
    protected override int Parallelism => 1;

    /// <inheritdoc />
    protected override JsonTypeInfo<DownloadVideoPayload> PayloadTypeInfo =>
        TaskPayloadContext.Default.DownloadVideoPayload;

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
