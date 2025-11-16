using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;
using PubSubHubbub;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using static System.StringComparison;

namespace Tubeshade.Server.Services;

public sealed class SubscriptionsService
{
    private readonly ILogger<SubscriptionsService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly ChannelSubscriptionRepository _channelSubscriptionRepository;
    private readonly IClock _clock;
    private readonly PubSubHubbubClient _pubSubHubbubClient;
    private readonly IOptionsMonitor<PubSubHubbubOptions> _optionsMonitor;

    public SubscriptionsService(
        ILogger<SubscriptionsService> logger,
        NpgsqlConnection connection,
        ChannelRepository channelRepository,
        ChannelSubscriptionRepository channelSubscriptionRepository,
        IClock clock,
        PubSubHubbubClient pubSubHubbubClient,
        IOptionsMonitor<PubSubHubbubOptions> optionsMonitor)
    {
        _logger = logger;
        _connection = connection;
        _channelRepository = channelRepository;
        _channelSubscriptionRepository = channelSubscriptionRepository;
        _clock = clock;
        _pubSubHubbubClient = pubSubHubbubClient;
        _optionsMonitor = optionsMonitor;
    }

    public async ValueTask<ChannelEntity> Subscribe(Guid channelId, Guid userId)
    {
        var currentTime = _clock.GetCurrentInstant();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await _channelRepository.GetAsync(channelId, userId, transaction);

        channel.SubscribedAt = currentTime;
        channel.ModifiedByUserId = userId;
        channel.ModifiedAt = currentTime;

        var updatedChannelCount = await _channelRepository.UpdateAsync(channel, transaction);
        Trace.Assert(updatedChannelCount is not 0);

        var options = _optionsMonitor.CurrentValue;
        if (options.CallbackBaseUri is null)
        {
            _logger.PubSubHubbubCallbackNotSet();
        }

        var host = new Uri(channel.ExternalUrl, UriKind.Absolute).Host;
        var isChannelFromYouTube = host.Equals("youtube.com", OrdinalIgnoreCase) || host.EndsWith(".youtube.com", OrdinalIgnoreCase);
        if (!isChannelFromYouTube)
        {
            _logger.ChannelNotFromYouTube(channel.ExternalUrl);
        }

        if (options.CallbackBaseUri is { } callbackBaseUri && isChannelFromYouTube)
        {
            var callbackUri = new UriBuilder(callbackBaseUri)
            {
                Path = $"/api/v1.0/YouTube/Notifications/{channelId}",
            }.Uri;

            var topicUri = new Uri($"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channel.ExternalId}", UriKind.Absolute);
            var verifyToken = Guid.NewGuid().ToString("N");
            var secret = options.Secret;

            var subscriptionId = await _channelSubscriptionRepository.AddAsync(
                new ChannelSubscriptionEntity
                {
                    Id = channelId,
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    Status = SubscriptionStatus.SubscriptionPending,
                    Callback = callbackUri.ToString(),
                    Topic = topicUri.ToString(),
                    VerifyToken = verifyToken,
                    Secret = secret,
                },
                transaction);

            Trace.Assert(subscriptionId is not null);
            await _pubSubHubbubClient.Subscribe(callbackUri, topicUri, secret, verifyToken);
        }

        await transaction.CommitAsync();

        return channel;
    }

    public async ValueTask<ChannelEntity> Unsubscribe(Guid channelId, Guid userId)
    {
        var currentTime = _clock.GetCurrentInstant();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await _channelRepository.GetAsync(channelId, userId, transaction);

        channel.SubscribedAt = null;
        channel.ModifiedByUserId = userId;
        channel.ModifiedAt = currentTime;

        var updatedChannelCount = await _channelRepository.UpdateAsync(channel, transaction);
        Trace.Assert(updatedChannelCount is not 0);

        var subscription = await _channelSubscriptionRepository.FindAsync(channelId, transaction);
        if (subscription is not null)
        {
            subscription.Status = SubscriptionStatus.UnsubscriptionPending;
            await _channelSubscriptionRepository.UpdateAsync(subscription, transaction);
        }

        await transaction.CommitAsync();

        if (subscription is not null)
        {
            await _pubSubHubbubClient.Unsubscribe(
                new Uri(subscription.Callback, UriKind.Absolute),
                new Uri(subscription.Topic, UriKind.Absolute),
                subscription.Secret,
                subscription.VerifyToken);
        }

        return channel;
    }

    public async ValueTask RefreshSubscriptions(CancellationToken cancellationToken)
    {
        await foreach (var subscription in _channelSubscriptionRepository.GetExpiringUnbufferedAsync().WithCancellation(cancellationToken))
        {
            await _pubSubHubbubClient.Subscribe(
                new(subscription.Callback, UriKind.Absolute),
                new(subscription.Topic, UriKind.Absolute),
                subscription.Secret,
                subscription.VerifyToken);
        }
    }
}
