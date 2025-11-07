using System;
using System.Threading.Channels;

namespace Tubeshade.Server.Services.Background;

internal readonly struct RunFinishedListener : IDisposable
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = true,
    });

    private readonly TaskListenerService _taskListenerService;

    public RunFinishedListener(TaskListenerService taskListenerService)
    {
        _taskListenerService = taskListenerService;
        Reader = _channel.Reader;

        _taskListenerService.TaskRunFinished += OnTaskRunFinished;
    }

    public ChannelReader<Guid> Reader { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _taskListenerService.TaskRunFinished -= OnTaskRunFinished;
        _channel.Writer.Complete();
    }

    private void OnTaskRunFinished(object? sender, Guid runId)
    {
        // SingleConsumerUnboundedChannel only fails to write if Writer.Complete() was called.
        // Even though the event is unsubscribed before completing the writer, the event handler is still called and fails.
        _ = _channel.Writer.TryWrite(runId);
    }
}
