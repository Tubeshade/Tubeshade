INSERT INTO identity.user_logins
    (provider_key,
     provider_display_name,
     user_id,
     login_provider,
     refresh_token)

VALUES (@ProviderKey,
        @ProviderDisplayName,
        @UserId,
        @LoginProvider,
        @RefreshToken);
