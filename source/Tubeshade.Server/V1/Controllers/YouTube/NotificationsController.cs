using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using PubSubHubbub;
using PubSubHubbub.Models;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Services;
using Tubeshade.Server.V1.Models;
using LoggerExtensions = Tubeshade.Server.Services.LoggerExtensions;

namespace Tubeshade.Server.V1.Controllers.YouTube;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/YouTube/[controller]/{channelId:guid}")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly ChannelSubscriptionRepository _channelSubscriptionRepository;
    private readonly VideoRepository _videoRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly IClock _clock;
    private readonly TaskService _taskService;
    private readonly UserRepository _userRepository;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ChannelRepository channelRepository,
        NpgsqlConnection connection,
        ChannelSubscriptionRepository channelSubscriptionRepository,
        VideoRepository videoRepository,
        PreferencesRepository preferencesRepository,
        IClock clock,
        TaskService taskService,
        UserRepository userRepository,
        ILogger<NotificationsController> logger)
    {
        _channelRepository = channelRepository;
        _connection = connection;
        _channelSubscriptionRepository = channelSubscriptionRepository;
        _clock = clock;
        _taskService = taskService;
        _userRepository = userRepository;
        _logger = logger;
        _videoRepository = videoRepository;
        _preferencesRepository = preferencesRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid channelId, IntentVerificationRequest request)
    {
        using (LoggerExtensions.IntentVerificationScope(_logger, request))
        {
            _logger.ReceivedIntentVerificationRequest(channelId);
        }

        if (!SubscriptionMode.TryFromName(request.Mode, true, out var mode))
        {
            ModelState.AddModelError("hub.mode", "Unexpected mode value");
            return BadRequest(ModelState);
        }

        var currentTime = _clock.GetCurrentInstant();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await _channelRepository.FindAsync(channelId, transaction);
        if (channel is null)
        {
            return Problem("Channel does not exist", statusCode: StatusCodes.Status404NotFound);
        }

        _logger.ReceivedIntentVerificationRequest(channel.Name);

        var subscription = await _channelSubscriptionRepository.FindAsync(channel.Id, transaction);
        if (subscription is null)
        {
            return Problem("Subscription request not found", statusCode: StatusCodes.Status404NotFound);
        }

        if (request.VerifyToken != subscription.VerifyToken)
        {
            ModelState.AddModelError("hub.verify_token", "Verify token does not match expected");
            return BadRequest(ModelState);
        }

        if (mode == SubscriptionMode.Subscribe)
        {
            if (subscription.Status != SubscriptionStatus.SubscriptionPending &&
                subscription.Status != SubscriptionStatus.Subscribed)
            {
                ModelState.AddModelError("hub.mode", "No pending subscription requests found");
                return BadRequest(ModelState);
            }

            subscription.Status = SubscriptionStatus.Subscribed;
            subscription.ExpiresAt = request.LeaseSeconds is { } lease
                ? currentTime.InUtc().PlusSeconds(lease).ToInstant()
                : null;

            await _channelSubscriptionRepository.UpdateAsync(subscription, transaction);
        }
        else if (mode == SubscriptionMode.Unsubscribe)
        {
            if (subscription.Status != SubscriptionStatus.UnsubscriptionPending)
            {
                ModelState.AddModelError("hub.mode", "No pending unsubscription requests found");
                return BadRequest(ModelState);
            }

            await _channelSubscriptionRepository.DeleteAsync(subscription, transaction);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected subscription mode {mode}");
        }

        await transaction.CommitAsync();
        return Ok(request.Challenge);
    }

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Xml)]
    public async Task<IActionResult> Post(Guid channelId, Feed feed)
    {
        using (LoggerExtensions.FeedUpdateScope(_logger, feed))
        {
            _logger.ReceivedFeedUpdate(channelId);
        }

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await _channelRepository.FindAsync(channelId, transaction);
        if (channel is null)
        {
            return Problem("Channel does not exist", statusCode: StatusCodes.Status404NotFound);
        }

        var videoUrl = feed.Entry.Link.Uri;
        _logger.ReceivedFeedUpdate(channel.Name, videoUrl);

        var userId = await _userRepository.GetSystemUserId(transaction);
        var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, transaction);
        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channelId, userId, CancellationToken.None);
        var existingVideo = await _videoRepository.FindByExternalUrl(videoUrl, userId, Access.Read, transaction);

        var videoType = existingVideo?.Type;
        if (videoType is null)
        {
            _ = VideoType.TryFromUrl(videoUrl, out videoType);
        }

        if (preferences is null ||
            videoType is null ||
            (videoType.Name is VideoType.Names.Video && preferences.VideosCount > 0) ||
            (videoType.Name is VideoType.Names.Livestream && preferences.LiveStreamsCount > 0) ||
            (videoType.Name is VideoType.Names.Short && preferences.ShortsCount > 0))
        {
            if (existingVideo is not null)
            {
                await _taskService.IndexVideo(userId, libraryId, existingVideo, transaction);
            }
            else
            {
                await _taskService.IndexVideo(userId, libraryId, videoUrl, transaction);
            }
        }
        else
        {
            _logger.FeedUpdatedIgnored(videoType.Name);
        }

        await transaction.CommitAsync();

        return NoContent();
    }
}
