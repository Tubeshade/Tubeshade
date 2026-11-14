WITH new_owner AS
    (INSERT INTO access.owners (created_by_user_id, modified_by_user_id, name, personal_for)
     SELECT users.id, users.id, users.name, users.id
     FROM identity.users
     WHERE users.id = @userId
     RETURNING id),

    own_grant AS
    (INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
     SELECT @userId, new_owner.id, @PersonalPermissions, @userId
     FROM new_owner),

    system_grant AS
    (INSERT INTO access.grants (principal_id, owner_id, permissions, granted_by)
     SELECT @userId, owners.id, @SystemPermissions, @userId
     FROM access.owners
         INNER JOIN identity.users ON owners.personal_for = users.id
     WHERE users.normalized_name = 'SYSTEM')

SELECT id FROM new_owner;
