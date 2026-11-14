DELETE FROM media.preferences
WHERE preferences.id IN (SELECT library_preferences.preference_id
                         FROM media.library_preferences
                         WHERE library_preferences.library_id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
                         UNION ALL
                         SELECT channel_preferences.preference_id
                         FROM media.channel_preferences
                             INNER JOIN media.channels ON channels.id = channel_preferences.channel_id
                         WHERE channels.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND preferences.id = @Id;
