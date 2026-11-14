UPDATE identity.users
SET modified_at         = CURRENT_TIMESTAMP,
    modified_by_user_id = @Id,
    name                = @Name,
    normalized_name     = @NormalizedName,
    password_hash       = @PasswordHash,
    security_stamp      = @SecurityStamp,
    concurrency_stamp   = @ConcurrencyStamp,
    two_factor_enabled  = @TwoFactorEnabled,
    lockout_end         = @LockoutEnd,
    lockout_enabled     = @LockoutEnabled,
    access_failed_count = @AccessFailedCount
WHERE users.id = @Id;
