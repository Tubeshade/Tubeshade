using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed class LibraryRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<LibraryEntity>(connection)
{
    /// <inheritdoc />
    protected override string TableName => "media.libraries";

    /// <inheritdoc />
    protected override string InsertSql =>
        $"""
         INSERT INTO media.libraries (created_by_user_id, modified_by_user_id, owner_id, name, storage_path) 
         VALUES (@CreatedByUserId, @ModifiedByUserId, @OwnerId, @Name, @StoragePath)
         RETURNING id;
         """;

    /// <inheritdoc />
    protected override string SelectSql =>
        $"""
         SELECT id AS Id,
                created_at AS CreatedAt,
                created_by_user_id AS CreatedByUserId,
                modified_at AS ModifiedAt,
                modified_by_user_id AS ModifiedByUserId,
                owner_id AS OwnerId,
                name AS Name,
                storage_path AS StoragePath
         FROM media.libraries
         """;

    /// <inheritdoc />
    protected override string UpdateSet =>
        $"""
           name = @Name,
           storage_path = @StoragePath
         """;
}
