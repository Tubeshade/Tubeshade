UPDATE media.videos
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    name = @Name,
    channel_id = (SELECT channels.id FROM media.channels WHERE channels.public_id = @ChannelId),
    storage_path = @StoragePath,
    external_id = @ExternalId,
    published_at = @PublishedAt,
    refreshed_at = @RefreshedAt,
    availability = @Availability,
    duration = @Duration,
    description = @Description,
    categories = @Categories,
    tags = @Tags,
    view_count = @ViewCount,
    like_count = @LikeCount,
    ignored_at = @IgnoredAt,
    ignored_by_user_id = @IgnoredByUserId
WHERE videos.owner_id IN (SELECT owner_id FROM access.accessible_owners(@ModifiedByUserId, @Permissions))
  AND videos.public_id = @PublicId;
