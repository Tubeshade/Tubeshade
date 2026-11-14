SELECT libraries.public_id
FROM media.libraries
    INNER JOIN media.library_channels ON libraries.id = library_channels.library_id
    INNER JOIN media.channels ON channels.id = library_channels.channel_id
WHERE channels.public_id = @id AND library_channels."primary" = true;
