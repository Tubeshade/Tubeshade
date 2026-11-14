INSERT INTO media.library_preferences (library_id, preference_id)
SELECT libraries.id, preferences.id
FROM media.libraries,
     media.preferences
WHERE libraries.public_id = @libraryId
  AND preferences.public_id = @preferenceId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
