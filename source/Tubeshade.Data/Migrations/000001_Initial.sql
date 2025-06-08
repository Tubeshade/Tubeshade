CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

DROP SCHEMA IF EXISTS identity CASCADE;
CREATE SCHEMA identity;

CREATE TYPE identity.access AS ENUM ('read', 'append', 'modify', 'delete', 'owner');

CREATE TABLE identity.users
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    name                text                                   NOT NULL,
    normalized_name     text                                   NOT NULL UNIQUE NOT DEFERRABLE,
    full_name           text                                   NULL,

    email               text                                   NOT NULL,
    normalized_email    text                                   NOT NULL UNIQUE NOT DEFERRABLE,
    email_confirmed     bool        DEFAULT false,

    password_hash       bytea                                  NULL,
    security_stamp      uuid                                   NULL,
    concurrency_stamp   uuid                                   NULL,
    two_factor_enabled  bool        DEFAULT false,

    lockout_end         timestamptz                            NULL,
    lockout_enabled     bool        DEFAULT false,
    access_failed_count int         DEFAULT 0
);

CREATE TABLE identity.user_logins
(
    provider_key          text NOT NULL PRIMARY KEY,
    user_id               uuid NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    login_provider        text NOT NULL,
    provider_display_name text NULL,
    refresh_token         text NULL
);
CREATE INDEX ON identity.user_logins (user_id, login_provider);

CREATE TABLE identity.owners
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    name                text                                   NOT NULL
);

CREATE TABLE identity.ownerships
(
    id                  uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id  uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    modified_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    owner_id            uuid                                   NOT NULL REFERENCES identity.owners (id) NOT DEFERRABLE,
    user_id             uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    access              identity.access                        NOT NULL,

    CONSTRAINT ownerships_owner_id_user_id_unique UNIQUE (owner_id, user_id, access) NOT DEFERRABLE
);

WITH id (id) AS (VALUES (uuid_generate_v4()))
INSERT
INTO identity.users (id, created_by_user_id, modified_by_user_id, name, normalized_name, email, normalized_email)
SELECT id.id,
       id.id,
       id.id,
       'system',
       'SYSTEM',
       'system@invalid.invalid',
       'SYSTEM@INVALID.INVALID'
FROM id
RETURNING id;

WITH system AS (SELECT id, name, normalized_name FROM identity.users WHERE normalized_name = 'SYSTEM')
INSERT
INTO identity.owners (id, created_by_user_id, modified_by_user_id, name)
SELECT system.id, system.id, system.id, system.name
FROM system
RETURNING id;

WITH system AS (SELECT id, name, normalized_name FROM identity.users WHERE normalized_name = 'SYSTEM')
INSERT
INTO identity.ownerships (id, created_by_user_id, modified_by_user_id, owner_id, user_id, access)
SELECT system.id, system.id, system.id, system.id, system.id, 'owner'
FROM system
RETURNING id;

CREATE TABLE identity.claims
(
    id                 uuid        DEFAULT uuid_generate_v4() NOT NULL PRIMARY KEY,
    created_at         timestamptz DEFAULT CURRENT_TIMESTAMP  NOT NULL,
    created_by_user_id uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    user_id            uuid                                   NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    claim_type         text                                   NOT NULL,
    claim_value        text                                   NULL
);

CREATE INDEX ON identity.claims (user_id, claim_type);
