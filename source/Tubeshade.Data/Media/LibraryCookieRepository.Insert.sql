WITH new_cookie AS
    (SELECT @Id AS id,
            @CreatedByUserId AS created_by_user_id,
            @ModifiedByUserId AS modified_by_user_id,
            @Domain AS domain,
            @Cookie AS cookie
     WHERE @Id IN (SELECT libraries.id FROM media.libraries WHERE libraries.owner_id IN (SELECT owner_id FROM access.accessible_owners(@PrincipalId, @Permissions))))

INSERT INTO media.library_external_cookies (id, created_by_user_id, modified_by_user_id, domain, cookie)
SELECT id, created_by_user_id, modified_by_user_id, domain, cookie FROM new_cookie
RETURNING id;
