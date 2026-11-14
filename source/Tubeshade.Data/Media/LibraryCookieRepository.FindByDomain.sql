SELECT cookies.id,
       cookies.created_at,
       cookies.created_by_user_id,
       cookies.modified_at,
       cookies.modified_by_user_id,
       cookies.domain,
       cookies.cookie
FROM media.library_external_cookies cookies
    INNER JOIN media.libraries ON cookies.id = libraries.id
WHERE libraries.id = @libraryId
  AND libraries.id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions)))
  AND domain = @domain;
