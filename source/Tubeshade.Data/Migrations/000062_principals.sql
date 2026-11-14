CREATE TYPE access.principal_kind AS ENUM ('user', 'application', 'system');

-- Grants and audit columns reference principals; a user shares its principal's id.
CREATE TABLE access.principals
(
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id  uuid                  NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    kind       access.principal_kind NOT NULL,
    created_at timestamptz           NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Existing users keep their id as the public id, since it is the subject of issued claims.
INSERT INTO access.principals (public_id, kind, created_at)
SELECT users.id,
       CASE WHEN users.normalized_name = 'SYSTEM' THEN 'system' ELSE 'user' END::access.principal_kind,
       users.created_at
FROM identity.users
ORDER BY users.created_at, users.id;
