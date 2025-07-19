using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services;

public sealed class ScanChannelBackgroundService : ChannelConsumerBackgroundService<YoutubeService, ScanChannelPayload>
{
    /// <inheritdoc />
    public ScanChannelBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.ScanChannel)
    {
    }

    /// <inheritdoc />
    protected override JsonTypeInfo<ScanChannelPayload> PayloadTypeInfo =>
        TaskPayloadContext.Default.ScanChannelPayload;

    /// <inheritdoc />
    protected override async ValueTask ProcessTaskPayload(
        TaskContext<YoutubeService, ScanChannelPayload> context,
        CancellationToken cancellationToken)
    {
        await context.Service.ScanChannel(
            context.Payload.LibraryId,
            context.Payload.ChannelId,
            context.Payload.UserId,
            context.TaskRepository,
            context.TaskRunId,
            context.Directory,
            cancellationToken);
    }
}
