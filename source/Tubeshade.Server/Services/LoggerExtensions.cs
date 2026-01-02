using System;
using Microsoft.Extensions.Logging;
using PubSubHubbub.Models;
using Tubeshade.Server.V1.Models;
using YoutubeDLSharp.Metadata;
using static Microsoft.Extensions.Logging.LogLevel;

namespace Tubeshade.Server.Services;

internal static partial class LoggerExtensions
{
    internal static readonly Func<ILogger, IntentVerificationRequest, IDisposable?> IntentVerificationScope =
        LoggerMessage.DefineScope<IntentVerificationRequest>("{@VerificationRequest}");

    internal static readonly Func<ILogger, Feed, IDisposable?> FeedUpdateScope =
        LoggerMessage.DefineScope<Feed>("{@Feed}");

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

    [LoggerMessage(17, Debug, "Starting to listen database notifications")]
    internal static partial void ListeningToDatabaseNotifications(this ILogger logger);

    [LoggerMessage(18, Debug, "Starting to listen on channel {NotificationChannel}")]
    internal static partial void ListeningToNotificationChannel(this ILogger logger, string notificationChannel);

    [LoggerMessage(19, Information, "Listening for database notifications from {Datasource}")]
    internal static partial void ListeningToDatabaseNotifications(this ILogger logger, string datasource);

    [LoggerMessage(20, Debug, "Received notification {NotificationChannel} from {NotificationPid}")]
    internal static partial void ReceivedNotification(this ILogger logger, string notificationChannel, int notificationPid);

    [LoggerMessage(21, Warning, "Unexpected notification channel {NotificationChannel}")]
    internal static partial void UnexpectedNotificationChannel(this ILogger logger, string notificationChannel);

    [LoggerMessage(22, Debug, "Received notification {NotificationChannel} with payload {NotificationPayload}")]
    internal static partial void ReceivedNotification(this ILogger logger, string notificationChannel, string notificationPayload);

    [LoggerMessage(23, Warning, "Failed to parse notification payload {NotificationPayload}")]
    internal static partial void UnexpectedNotificationPayload(this ILogger logger, string notificationPayload);

    [LoggerMessage(24, Information, "Queuing notification {NotificationChannel} {NotificationPayload}")]
    internal static partial void QueueingNotification(this ILogger logger, string notificationChannel, Guid notificationPayload);

    [LoggerMessage(25, Debug, "Received intent verification request for channel {ChannelId}")]
    internal static partial void ReceivedIntentVerificationRequest(this ILogger logger, Guid channelId);

    [LoggerMessage(26, Information, "Received intent verification request for channel {ChannelName}")]
    internal static partial void ReceivedIntentVerificationRequest(this ILogger logger, string channelName);

    [LoggerMessage(27, Debug, "Received feed update notification for channel {ChannelId}")]
    internal static partial void ReceivedFeedUpdate(this ILogger logger, Guid channelId);

    [LoggerMessage(28, Information, "Received feed update notification for channel {ChannelName} video {VideoUrl}")]
    internal static partial void ReceivedFeedUpdate(this ILogger logger, string channelName, string videoUrl);

    [LoggerMessage(29, Information, "Not indexing video of type {VideoType} due to preferences")]
    internal static partial void FeedUpdatedIgnored(this ILogger logger, string videoType);

    [LoggerMessage(30, Trace, "Not updating existing locked segment {SegmentId} {SegmentExternalId}")]
    internal static partial void LockedSponsorBlockSegment(this ILogger logger, Guid segmentId, string segmentExternalId);

    [LoggerMessage(31, Debug, "Updating existing segment {SegmentId} {SegmentExternalId}")]
    internal static partial void UpdatingSponsorBlockSegment(this ILogger logger, Guid segmentId, string segmentExternalId);

    [LoggerMessage(32, Debug, "Adding new segment {SegmentExternalId}")]
    internal static partial void AddingSponsorBlockSegment(this ILogger logger, string segmentExternalId);

    [LoggerMessage(33, Debug, "Deleting existing segment {SegmentId} {SegmentExternalId}")]
    internal static partial void DeletingSponsorBlockSegment(this ILogger logger, Guid segmentId, string segmentExternalId);

    [LoggerMessage(34, Debug, "Indexing video {VideoExternalId}")]
    internal static partial void IndexingVideo(this ILogger logger, string videoExternalId);

    [LoggerMessage(35, Debug, "Deleting indexed but not downloaded video file {VideoFileId}")]
    internal static partial void DeletingNotDownloadedFile(this ILogger logger, Guid videoFileId);

    [LoggerMessage(36, Debug, "No formats found for {FormatFilter}")]
    internal static partial void NoFormats(this ILogger logger, string formatFilter);

    [LoggerMessage(37, Debug, "Selected {DistinctCount} distinct video formats from {Count}")]
    internal static partial void SelectedFormats(this ILogger logger, int distinctCount, int count);

    [LoggerMessage(38, Debug, "Skipped format filter {FormatFilter}")]
    internal static partial void SkippedFormat(this ILogger logger, string formatFilter);

    [LoggerMessage(39, Debug, "Selected format filter {FormatFilter}")]
    internal static partial void SelectedFormat(this ILogger logger, string formatFilter);

    [LoggerMessage(40, Debug, "Creating new file for format {FormatFilter}")]
    internal static partial void CreatingFileForFormat(this ILogger logger, string formatFilter);

    [LoggerMessage(41, Debug, "Found existing file {FileId} for filter {FormatFilter}")]
    internal static partial void ExistingFileForFormat(this ILogger logger, Guid fileId, string formatFilter);

    [LoggerMessage(42, Debug, "Found existing files for filter {FormatFilter}")]
    internal static partial void ExistingFilesForFormat(this ILogger logger, string formatFilter);

    [LoggerMessage(43, Debug, "Writing video chapters to {Path}")]
    internal static partial void WritingVideoChapters(this ILogger logger, string path);

    [LoggerMessage(44, Information, "Starting scheduler with period {SchedulerPeriod}")]
    internal static partial void StartingScheduler(this ILogger logger, TimeSpan schedulerPeriod);

    [LoggerMessage(45, Trace, "Scheduler tick")]
    internal static partial void SchedulerTick(this ILogger logger);

    [LoggerMessage(46, Trace, "Skipping schedule")]
    internal static partial void SkippingSchedule(this ILogger logger);

    [LoggerMessage(47, Debug, "Starting task {TaskId} based on schedule")]
    internal static partial void StartingScheduledTask(this ILogger logger, Guid taskId);

    [LoggerMessage(48, Trace, "Scheduler period has not changed")]
    internal static partial void SchedulerPeriodUnchanged(this ILogger logger);

    [LoggerMessage(49, Information, "Updating scheduler period from {OldSchedulerPeriod} to {SchedulerPeriod}")]
    internal static partial void SchedulerPeriodChanged(this ILogger logger, TimeSpan oldSchedulerPeriod, TimeSpan schedulerPeriod);

    [LoggerMessage(50, Debug, "Thumbnail already exists in the same or higher resolution")]
    internal static partial void ExistingThumbnail(this ILogger logger);

    [LoggerMessage(51, Debug, "Creating new thumbnail for video")]
    internal static partial void CreatingThumbnail(this ILogger logger);

    [LoggerMessage(52, Debug, "Updating existing thumbnail {ThumbnailId}")]
    internal static partial void UpdatingExistingThumbnail(this ILogger logger, Guid thumbnailId);

    [LoggerMessage(53, Trace, "Video already existed, not downloading")]
    internal static partial void NotDownloadingExistingVideo(this ILogger logger);

    [LoggerMessage(54, Trace, "Preferences set to not automatically download videos")]
    internal static partial void NotDownloadingDueToPreferences(this ILogger logger);

    [LoggerMessage(55, Debug, "Newer video {VideoId} is already downloaded, not downloading")]
    internal static partial void NotDownloadingOldVideo(this ILogger logger, Guid videoId);

    [LoggerMessage(56, Trace, "Trying to find user by login provider key")]
    internal static partial void SearchingUserByProviderKey(this ILogger logger);

    [LoggerMessage(57, Warning, "Claims principal from {LoginProvider} does not have a name identifier claim")]
    internal static partial void ProviderMissingNameClaim(this ILogger logger, string loginProvider);

    [LoggerMessage(58, Trace, "Trying to find user by id")]
    internal static partial void SearchingUserById(this ILogger logger);

    [LoggerMessage(59, Debug, "Creating new channel {ChannelName} ({ExternalId})")]
    internal static partial void CreatingChannel(this ILogger logger, string channelName, string externalId);

    [LoggerMessage(60, Trace, "{Executable}: {StandardError}")]
    internal static partial void StandardError(this ILogger logger, string executable, string standardError);

    [LoggerMessage(61, Trace, "{Executable}: {StandardOutput}")]
    internal static partial void StandardOutput(this ILogger logger, string executable, string standardOutput);

    [LoggerMessage(62, Information, "Getting metadata for unknown url {ExternalUrl}")]
    internal static partial void UnknownUrlMetadata(this ILogger logger, string externalUrl);

    [LoggerMessage(63, Information, "Downloading file {FileId} with combined format {FormatId} in {ContainerType}")]
    internal static partial void DownloadingCombinedVideoFile(this ILogger logger, Guid fileId, string formatId, string containerType);

    [LoggerMessage(64, Information, "Downloading file {FileId} with video {VideoFormatId} and audio {AudioFormatId} in {ContainerType}")]
    internal static partial void DownloadingSplitVideoFile(this ILogger logger, Guid fileId, string videoFormatId, string audioFormatId, string containerType);

    [LoggerMessage(65, Information, "Video file {FileId} already exists")]
    internal static partial void ExistingVideoFile(this ILogger logger, Guid fileId);

    [LoggerMessage(66, Debug, "Downloading file {FileId} with audio rate {AudioLimitRate} and video rate {VideoLimitRate}")]
    internal static partial void SplitFormatLimitRates(this ILogger logger, Guid fileId, long? audioLimitRate, long? videoLimitRate);

    [LoggerMessage(67, Information, "Downloaded file {FileId}")]
    internal static partial void DownloadedVideoFile(this ILogger logger, Guid fileId);

    [LoggerMessage(68, Debug, "Moving file from {SourcePath} to {TargetPath}")]
    internal static partial void MovingFile(this ILogger logger, string sourcePath, string targetPath);

    [LoggerMessage(69, Debug, "Completed split downloads")]
    internal static partial void CompletedDownloadTasks(this ILogger logger);

    [LoggerMessage(70, Debug, "Finished copying split download data to FIFO streams")]
    internal static partial void CompletedCopyTasks(this ILogger logger);

    [LoggerMessage(71, Debug, "Flushed FIFO streams")]
    internal static partial void FlushedFifoStreams(this ILogger logger);

    [LoggerMessage(72, Debug, "Closed all FIFO streams used for split video streams")]
    internal static partial void ClosedFifoStreams(this ILogger logger);

    [LoggerMessage(73, Debug, "Finished combining split video streams")]
    internal static partial void FinishedCombiningSplitFile(this ILogger logger);

    [LoggerMessage(74, Debug, "Copying file from {SourcePath} to {TargetPath} with ffmpeg")]
    internal static partial void MovingFileFfmpeg(this ILogger logger, string sourcePath, string targetPath);

    [LoggerMessage(75, Debug, "Video {ExternalId} was live {WasLive}, is live {IsLive}, with status {LiveStatus}")]
    internal static partial void VideoLiveStatus(this ILogger logger, string externalId, bool? wasLive, bool? isLive, LiveStatus? liveStatus);

    [LoggerMessage(76, Information, "Queuing notification {NotificationChannel} {TaskId} {TaskSource}")]
    internal static partial void QueueingNotification(this ILogger logger, string notificationChannel, Guid taskId, string taskSource);

    [LoggerMessage(77, Debug, "Video {VideoId} is still live, not downloading")]
    internal static partial void NotDownloadingLiveVideo(this ILogger logger, Guid videoId);
}
