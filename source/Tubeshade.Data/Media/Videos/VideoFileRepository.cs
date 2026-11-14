using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Media.Videos;

public sealed partial class VideoFileRepository(NpgsqlConnection connection)
    : ModifiableRepositoryBase<VideoFileEntity>(connection)
{
    /// <remarks>A file takes the owner of its video, so <see cref="VideoFileEntity.OwnerId"/> is not used.</remarks>
    protected override string InsertSql => Queries.Insert;

    protected override string GetSql => Queries.Get;

    protected override string FindSql => Queries.Find;

    protected override string GetForSystemSql => Queries.GetForSystem;

    protected override string FindForSystemSql => Queries.FindForSystem;

    protected override string UpdateSql => Queries.Update;

    protected override string DeleteSql => Queries.Delete;

    /// <summary>Records where the file is being downloaded to, if the user holds Update on the file.</summary>
    public async ValueTask<bool> CreateTemporaryFile(
        Guid id,
        long userId,
        Guid taskRunId,
        string path,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            Queries.CreateTemporaryFile,
            Access.Parameters(new UserPrincipal(userId), Resources.File, Permissions.Update, new { id, taskRunId, path }),
            cancellationToken: cancellationToken);

        return await Connection.ExecuteAsync(command) is 1;
    }
}
