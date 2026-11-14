WITH inserted AS
    (INSERT INTO media.library_external_cookies (library_id, created_by_user_id, modified_by_user_id, domain, cookie)
     SELECT libraries.id, @CreatedByUserId, @ModifiedByUserId, @Domain, @Cookie
     FROM media.libraries
     WHERE libraries.public_id = @PublicId
       AND libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))
     RETURNING library_id)

SELECT libraries.public_id
FROM inserted
    INNER JOIN media.libraries ON libraries.id = inserted.library_id;
