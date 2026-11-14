WITH accessible AS
    (SELECT libraries.id,
            libraries.public_id,
            libraries.owner_id
     FROM media.libraries
     WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
       AND storage_path IN (SELECT storage_path FROM media.libraries WHERE public_id = @newLibraryId)),

     target AS
    (SELECT accessible.id,
            accessible.owner_id
     FROM accessible
     WHERE accessible.public_id = @newLibraryId),

     -- the channel takes the owner of the new library, and moving it to another owner needs @MovePermissions on the current one
     channel AS
    (SELECT channels.id
     FROM media.channels
     WHERE channels.public_id = @channelId
       AND (channels.owner_id IN (SELECT target.owner_id FROM target) OR
            channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @MovePermissions)))),

     moved AS
    (UPDATE media.library_channels
     SET library_id = target.id
     FROM target,
          channel
     WHERE library_channels.channel_id = channel.id
       AND library_channels."primary"
       AND EXISTS(SELECT 1 FROM accessible WHERE accessible.id = library_channels.library_id)
     RETURNING library_channels.channel_id, target.owner_id),

     moved_channels AS
    (UPDATE media.channels
     SET owner_id = moved.owner_id
     FROM moved
     WHERE channels.id = moved.channel_id
       AND channels.owner_id != moved.owner_id),

     moved_videos AS
    (UPDATE media.videos
     SET owner_id = moved.owner_id
     FROM moved
     WHERE videos.channel_id = moved.channel_id
       AND videos.owner_id != moved.owner_id),

     moved_files AS
    (UPDATE media.video_files
     SET owner_id = moved.owner_id
     FROM moved,
          media.videos
     WHERE video_files.video_id = videos.id
       AND videos.channel_id = moved.channel_id
       AND video_files.owner_id != moved.owner_id)

SELECT count(*)::integer
FROM moved;
