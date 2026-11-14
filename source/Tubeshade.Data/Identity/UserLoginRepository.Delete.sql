DELETE FROM identity.user_logins
WHERE user_id = @UserId AND provider_key = @ProviderKey;
