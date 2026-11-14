WITH accessible AS
    (SELECT libraries.id
     FROM media.libraries
     WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
       AND storage_path IN (SELECT storage_path FROM media.libraries WHERE id = @newLibraryId))

UPDATE media.library_channels
SET library_id = @newLibraryId
WHERE channel_id = @channelId AND
      "primary" AND
      EXISTS(SELECT 1 FROM accessible WHERE accessible.id = library_id) AND
      EXISTS(SELECT 1 FROM accessible WHERE accessible.id = @newLibraryId);
