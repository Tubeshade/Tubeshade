UPDATE identity.user_logins
SET refresh_token = @refreshToken
WHERE provider_key = @providerKey AND login_provider = @loginProvider;
