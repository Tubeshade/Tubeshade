using System;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services.Background;

public sealed class IndexBackgroundService : TaskBackgroundServiceBase<YoutubeService, IndexPayload>
{
    /// <inheritdoc />
    public IndexBackgroundService(IServiceProvider serviceProvider)
        : base(serviceProvider, TaskType.Index, TaskPayloadContext.Default.IndexPayload)
    {
    }

    /// <inheritdoc />
    protected override int Parallelism => 4;

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
