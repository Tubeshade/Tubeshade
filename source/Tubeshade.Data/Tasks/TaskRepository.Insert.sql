-- A task for a library belongs to the library's owner, otherwise to the given owner
WITH target AS (SELECT COALESCE((SELECT libraries.owner_id FROM media.libraries WHERE libraries.public_id = @LibraryId), @OwnerId) AS owner_id)

INSERT INTO tasks.tasks (created_by_user_id, modified_by_user_id, owner_id, type, user_id, library_id, channel_id, playlist_id, video_id, url, all_videos, payload)
SELECT @CreatedByUserId,
       @ModifiedByUserId,
       target.owner_id,
       @Type,
       @UserId,
       (SELECT libraries.id FROM media.libraries WHERE libraries.public_id = @LibraryId),
       (SELECT channels.id FROM media.channels WHERE channels.public_id = @ChannelId),
       (SELECT playlists.id FROM media.playlists WHERE playlists.public_id = @PlaylistId),
       (SELECT videos.id FROM media.videos WHERE videos.public_id = @VideoId),
       @Url,
       @AllVideos,
       @Payload
FROM target
WHERE target.owner_id IN (SELECT owner_id FROM access.accessible_owners(@CreatedByUserId, @Permissions))
RETURNING public_id;
