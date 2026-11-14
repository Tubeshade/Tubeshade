SELECT id, public_id, created_at, created_by_user_id, user_id, claim_type, claim_value
FROM identity.claims
WHERE claims.user_id = @userId;
