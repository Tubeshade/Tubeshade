SELECT users.id,
       users.created_at,
       users.created_by_user_id,
       users.modified_at,
       users.modified_by_user_id,
       users.name,
       users.normalized_name,
       users.password_hash,
       users.security_stamp,
       users.concurrency_stamp,
       users.two_factor_enabled,
       users.lockout_end,
       users.lockout_enabled,
       users.access_failed_count
FROM identity.users
INNER JOIN identity.user_logins ON users.id = user_logins.user_id
WHERE provider_key = @providerKey
  AND login_provider = @loginProvider;
