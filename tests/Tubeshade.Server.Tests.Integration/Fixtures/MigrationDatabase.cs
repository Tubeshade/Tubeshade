using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using DbUp;
using Npgsql;
using Tubeshade.Data.Migrations;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

/// <summary>A separate database for testing how migrations upgrade existing data.</summary>
internal static partial class MigrationDatabase
{
    internal static async Task<string> CreateAsync(ServerFixture fixture)
    {
        var databaseName = $"upgrade_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.ExecuteAsync($"CREATE DATABASE {databaseName};");

        return new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = databaseName }.ConnectionString;
    }

    /// <summary>Applies the migrations up to and including <paramref name="lastMigration"/>, or all of them.</summary>
    internal static void Migrate(string connectionString, int? lastMigration = null)
    {
        var result = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrationService).Assembly,
                name => lastMigration is null || MigrationNumber(name) <= lastMigration)
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        result.Successful.Should().BeTrue(result.Error?.ToString());
        if (lastMigration is not null)
        {
            result.Scripts.Should().Contain(script => script.Name.Contains($"{lastMigration:D6}_"));
        }
    }

    private static int MigrationNumber(string scriptName)
    {
        var match = MigrationNumberRegex().Match(scriptName);
        return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
    }

    [GeneratedRegex(@"\.Migrations\.(\d{6})_")]
    private static partial Regex MigrationNumberRegex();
}
