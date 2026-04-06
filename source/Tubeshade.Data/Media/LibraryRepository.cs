using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed class LibraryRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<LibraryEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.libraries";

    /// <inheritdoc />
    protected override string InsertSql =>
        """
        INSERT INTO media.libraries (created_by_user_id, modified_by_user_id, owner_id, name, storage_path, subscriptions_schedule_id) 
        VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath, @SubscriptionsScheduleId)
        RETURNING id;
        """;

    /// <inheritdoc />
    protected override string SelectSql =>
        """
        SELECT id,
               created_at,
               created_by_user_id,
               modified_at,
               modified_by_user_id,
               owner_id,
               name,
               storage_path,
               subscriptions_schedule_id
        FROM media.libraries
        """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        """
          name = @Name,
          storage_path = @StoragePath,
          subscriptions_schedule_id = @SubscriptionsScheduleId
        """;
}
