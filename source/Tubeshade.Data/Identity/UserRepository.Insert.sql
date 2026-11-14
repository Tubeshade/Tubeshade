WITH system AS (SELECT id FROM identity.users WHERE normalized_name = 'SYSTEM')
INSERT
INTO identity.users (
            id,
            created_by_user_id,
            modified_by_user_id,
            name,
            normalized_name,
            password_hash,
            security_stamp,
            concurrency_stamp,
            two_factor_enabled,
            lockout_end,
            lockout_enabled,
            access_failed_count)
SELECT @Id,
       system.id,
       system.id,
       @Name,
       @NormalizedName,
       @PasswordHash,
       @SecurityStamp,
       @ConcurrencyStamp,
       @TwoFactorEnabled,
       @LockoutEnd,
       @LockoutEnabled,
       @AccessFailedCount
FROM system
RETURNING id;
