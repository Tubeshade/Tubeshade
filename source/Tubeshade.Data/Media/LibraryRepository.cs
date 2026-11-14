using Npgsql;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Media;

public sealed partial class LibraryRepository(NpgsqlConnection connection) : ModifiableRepositoryBase<LibraryEntity>(connection)
{
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;
}
