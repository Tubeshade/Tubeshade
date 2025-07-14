ALTER TABLE identity.users
    DROP COLUMN full_name,
    DROP COLUMN email,
    DROP COLUMN normalized_email,
    DROP COLUMN email_confirmed;
