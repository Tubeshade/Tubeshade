using System;
using System.Threading.Channels;

namespace Tubeshade.Server.Services.Background;

internal sealed class RunFinishedListener : IDisposable
{
    private readonly Channel<Guid?> _channel = Channel.CreateUnbounded<Guid?>(new UnboundedChannelOptions
    {
        AllowSynchronousContinuations = true,
        SingleReader = true,
        SingleWriter = true,
    });

    private readonly TaskListenerService _taskListenerService;

    internal RunFinishedListener(TaskListenerService taskListenerService)
    {
        _taskListenerService = taskListenerService;
        Reader = _channel.Reader;

        _taskListenerService.TaskRunFinished += OnTaskRunFinished;
        _taskListenerService.ListeningResumed += OnListeningResumed;
    }

    /// <summary>
    /// Ids of finished task runs, or <see langword="null"/> if notifications might have been missed,
    /// in which case the finished task runs need to be queried instead.
    /// </summary>
    internal ChannelReader<Guid?> Reader { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _taskListenerService.TaskRunFinished -= OnTaskRunFinished;
        _taskListenerService.ListeningResumed -= OnListeningResumed;
        _channel.Writer.Complete();
    }

    private void OnTaskRunFinished(object? sender, Guid runId)
    {
        _ = _channel.Writer.TryWrite(runId);
    }

    private void OnListeningResumed(object? sender, EventArgs args)
    {
        _ = _channel.Writer.TryWrite(null);
    }
}
