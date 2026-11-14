SELECT provider_key, provider_display_name, user_id, login_provider, refresh_token
FROM identity.user_logins
WHERE user_id = @userId;
