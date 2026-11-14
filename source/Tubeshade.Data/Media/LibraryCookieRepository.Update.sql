UPDATE media.library_external_cookies
SET modified_at = CURRENT_TIMESTAMP,
    modified_by_user_id = @ModifiedByUserId,
    domain = @Domain,
    cookie = @Cookie
WHERE id = @Id
  AND id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)));
