INSERT INTO media.videos (created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, type, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration, ignored_at, ignored_by_user_id)
SELECT @CreatedByUserId, @ModifiedByUserId, channels.owner_id, @Name, @Description, @Categories, @Tags, @Type, @ViewCount, @LikeCount, channels.id, @StoragePath, @ExternalId, @ExternalUrl, @PublishedAt, @RefreshedAt, @Availability, @Duration, @IgnoredAt, @IgnoredByUserId
FROM media.channels
WHERE channels.public_id = @ChannelId
  AND channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
