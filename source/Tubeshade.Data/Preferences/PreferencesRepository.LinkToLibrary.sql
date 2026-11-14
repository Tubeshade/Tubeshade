INSERT INTO media.library_preferences (library_id, preference_id)
SELECT libraries.id, @preferenceId
FROM media.libraries
WHERE libraries.id = @libraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
