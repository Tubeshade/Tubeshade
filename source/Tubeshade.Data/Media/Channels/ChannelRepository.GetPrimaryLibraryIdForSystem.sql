SELECT id
FROM media.libraries
INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
WHERE library_channels.channel_id = @id AND library_channels."primary" = true;
