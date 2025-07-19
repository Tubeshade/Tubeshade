using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services;

public sealed class IndexBackgroundService : ChannelConsumerBackgroundService<YoutubeService, IndexPayload>
{
    /// <inheritdoc />
    public IndexBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.Index)
    {
    }

    /// <inheritdoc />
    protected override int Parallelism => 4;

    /// <inheritdoc />
    protected override JsonTypeInfo<IndexPayload> PayloadTypeInfo => TaskPayloadContext.Default.IndexPayload;

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, IndexPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.Index(
            context.Payload.Url,
            context.Payload.LibraryId,
            context.Payload.UserId,
            context.Directory,
            cancellationToken);
    }
}
