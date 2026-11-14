UPDATE media.library_external_cookies
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    domain = @Domain,
    cookie = @Cookie
FROM media.libraries
WHERE libraries.id = library_external_cookies.library_id
  AND libraries.public_id = @PublicId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions));
