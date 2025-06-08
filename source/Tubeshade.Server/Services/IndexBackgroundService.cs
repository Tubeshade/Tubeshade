using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services;

public sealed class IndexBackgroundService : ChannelConsumerBackgroundService<YoutubeService, IndexVideoPayload>
{
    /// <inheritdoc />
    public IndexBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskBackgroundService.IndexTaskChannel.Reader, TaskType.IndexVideo)
    {
    }

    /// <inheritdoc />
    protected override int Parallelism => 4;

    /// <inheritdoc />
    protected override JsonTypeInfo<IndexVideoPayload> PayloadTypeInfo =>
        TaskPayloadContext.Default.IndexVideoPayload;

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, IndexVideoPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.IndexVideo(
            context.Payload.VideoUrl,
            context.Payload.LibraryId,
            context.Payload.UserId,
            context.TaskRepository,
            context.TaskRunId,
            context.Directory,
            cancellationToken);
    }
}
