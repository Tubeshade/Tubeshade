using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using PubSubHubbub;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Server.Services;
using Tubeshade.Server.V1.Models;
using LoggerExtensions = Tubeshade.Server.Services.LoggerExtensions;

namespace Tubeshade.Server.V1.Controllers.YouTube;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/YouTube/[controller]/{channelId:guid}")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly ChannelSubscriptionRepository _channelSubscriptionRepository;
    private readonly UserRepository _userRepository;
    private readonly TaskService _taskService;

    public NotificationsController(
        ILogger<NotificationsController> logger,
        IClock clock,
        NpgsqlConnection connection,
        ChannelRepository channelRepository,
        ChannelSubscriptionRepository channelSubscriptionRepository,
        UserRepository userRepository,
        TaskService taskService)
    {
        _logger = logger;
        _clock = clock;
        _connection = connection;
        _channelRepository = channelRepository;
        _channelSubscriptionRepository = channelSubscriptionRepository;
        _userRepository = userRepository;
        _taskService = taskService;
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
    public async Task<IActionResult> Post(Guid channelId, CancellationToken cancellationToken)
    {
        _logger.ReceivedFeedUpdate(channelId);

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var channel = await _channelRepository.FindAsync(channelId, transaction);
        if (channel is null)
        {
            return Problem("Channel does not exist", statusCode: StatusCodes.Status404NotFound);
        }

        _logger.ReceivedFeedUpdate(channelId, channel.Name);

        var userId = await _userRepository.GetSystemUserId(transaction);
        var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, transaction);
        await transaction.CommitAsync(cancellationToken);

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        await _taskService.FeedUpdate(userId, libraryId, channelId, payload);

        return NoContent();
    }
}
