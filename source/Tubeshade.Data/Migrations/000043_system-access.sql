-- Delete ownerships that give users owner access to system data
WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
DELETE
FROM identity.ownerships
WHERE EXISTS(SELECT 1 FROM system WHERE system.id = owner_id)
  AND NOT EXISTS(SELECT 1 FROM system WHERE system.id = user_id)
  AND access = 'owner';

-- Give system user owner access to all user data
WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     all_users AS (SELECT id FROM identity.users WHERE normalized_name != 'SYSTEM')
INSERT
INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
SELECT all_users.id, all_users.id, all_users.id, system.id, 'owner'
FROM all_users,
     system
ON CONFLICT (owner_id, user_id, access) DO NOTHING
RETURNING id, owner_id, user_id, access;

-- Give all users read access to system data
WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM'),
     all_users AS (SELECT id FROM identity.users WHERE normalized_name != 'SYSTEM')
INSERT
INTO identity.ownerships (created_by_user_id, modified_by_user_id, owner_id, user_id, access)
SELECT all_users.id, all_users.id, system.id, all_users.id, 'read'
FROM all_users,
     system
ON CONFLICT (owner_id, user_id, access) DO NOTHING
RETURNING id, owner_id, user_id, access;
