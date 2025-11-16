using System;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;

namespace Tubeshade.Server.Services;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, Debug, "Created temporary directory {Path}")]
    internal static partial void Created(this ILogger logger, string path);

    [LoggerMessage(2, Debug, "Deleting temporary directory {Path}")]
    internal static partial void Deleting(this ILogger logger, string path);

    [LoggerMessage(3, Trace, """
                             ffmpeg output:
                             {Output}
                             {Error}
                             """)]
    internal static partial void FfmpegOutput(this ILogger logger, string? output, string? error);

    [LoggerMessage(4, Debug, "Copying existing audio stream")]
    internal static partial void CopyingAudio(this ILogger logger);

    [LoggerMessage(5, Debug, "Transcoding audio to {AudioCodec} at {AudioBitRate}")]
    internal static partial void TranscodingAudio(this ILogger logger, string audioCodec, string audioBitRate);

    [LoggerMessage(6, Debug, "Received created task {TaskId}")]
    internal static partial void ReceivedCreatedTask(this ILogger logger, Guid taskId);

    [LoggerMessage(7, Debug, "Could not dequeue task {TaskId}")]
    internal static partial void CouldNotDequeueTask(this ILogger logger, Guid taskId);

    [LoggerMessage(8, Debug, "Adding task run for {TaskId}")]
    internal static partial void AddingTaskRun(this ILogger logger, Guid taskId);

    [LoggerMessage(9, Debug, "{Count} task runs are blocking {TaskRunId}")]
    internal static partial void BlockingTaskRuns(this ILogger logger, int count, Guid taskRunId);

    [LoggerMessage(10, Debug, "Removed task run {finishedTaskRunId} which was blocking {taskRunId}, {Count} remaining")]
    internal static partial void RemovedBlockingTaskRun(this ILogger logger, Guid finishedTaskRunId, Guid taskRunId, int count);

    [LoggerMessage(11, Trace, "Removed task run {finishedTaskRunId} which was not blocking {taskRunId}")]
    internal static partial void NonBlockingTaskRun(this ILogger logger, Guid finishedTaskRunId, Guid taskRunId);

    [LoggerMessage(12, Debug, "Starting task run {TaskRunId} for {TaskId}")]
    internal static partial void StartingTaskRun(this ILogger logger, Guid taskRunId, Guid taskId);

    [LoggerMessage(13, Warning, "Task cancelled")]
    internal static partial void TaskCancelled(this ILogger logger, Exception exception);

    [LoggerMessage(14, Error, "Task failed unexpectedly")]
    internal static partial void TaskFailed(this ILogger logger, Exception exception);

    [LoggerMessage(15, Debug, "PubSubHubbub callback base uri is not set")]
    internal static partial void PubSubHubbubCallbackNotSet(this ILogger logger);

    [LoggerMessage(16, Debug, "Channel {ExternalUrl} is not from YouTube")]
    internal static partial void ChannelNotFromYouTube(this ILogger logger, string externalUrl);
}
