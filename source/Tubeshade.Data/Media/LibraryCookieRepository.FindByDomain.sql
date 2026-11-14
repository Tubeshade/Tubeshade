SELECT cookies.library_id AS id,
       libraries.public_id,
       cookies.created_at,
       cookies.created_by_user_id,
       cookies.modified_at,
       cookies.modified_by_user_id,
       cookies.domain,
       cookies.cookie
FROM media.library_external_cookies cookies
    INNER JOIN media.libraries ON cookies.library_id = libraries.id
WHERE libraries.public_id = @libraryId
  AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
  AND domain = @domain;
