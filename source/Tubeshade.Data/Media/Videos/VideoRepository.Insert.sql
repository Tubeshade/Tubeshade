INSERT INTO media.videos (created_by_user_id, modified_by_user_id, owner_id, name, description, categories, tags, type, view_count, like_count, channel_id, storage_path, external_id, external_url, published_at, refreshed_at, availability, duration, ignored_at, ignored_by_user_id)
SELECT @CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @Description, @Categories, @Tags, @Type, @ViewCount, @LikeCount, @ChannelId, @StoragePath, @ExternalId, @ExternalUrl, @PublishedAt, @RefreshedAt, @Availability, @Duration, @IgnoredAt, @IgnoredByUserId
WHERE @OwnerId IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING id;
