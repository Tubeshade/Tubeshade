using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AwesomeAssertions;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Data;

public sealed partial class QueryPreparationTests(ServerFixture fixture) : ServerTests(fixture)
{
    // PostgreSQL does not infer the type of a parameter that is first used as "@x IS NULL", and Dapper sends typed parameters.
    private static readonly Dictionary<string, NpgsqlDbType> ParameterTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["channelIds"] = NpgsqlDbType.Array | NpgsqlDbType.Uuid,
        ["videoIds"] = NpgsqlDbType.Array | NpgsqlDbType.Uuid,
        ["externalIds"] = NpgsqlDbType.Array | NpgsqlDbType.Text,
        ["ExternalId"] = NpgsqlDbType.Text,
        ["Url"] = NpgsqlDbType.Text,
        ["Query"] = NpgsqlDbType.Text,
        ["AllVideos"] = NpgsqlDbType.Boolean,
        ["PrincipalId"] = NpgsqlDbType.Bigint,
        ["CreatedByUserId"] = NpgsqlDbType.Bigint,
        ["ModifiedByUserId"] = NpgsqlDbType.Bigint,
        ["IgnoredByUserId"] = NpgsqlDbType.Bigint,
        ["DownloadedByUserId"] = NpgsqlDbType.Bigint,
        ["UserId"] = NpgsqlDbType.Bigint,
        ["OwnerId"] = NpgsqlDbType.Bigint,
        ["taskRequestId"] = NpgsqlDbType.Bigint,
    };

    // Queries that take internal ids by a name that is otherwise a public id.
    private static readonly Dictionary<(string Query, string Parameter), NpgsqlDbType> QueryParameterTypes = new()
    {
        [("Media.Channels.ChannelSubscriptionRepository.DeleteForSystem", "id")] = NpgsqlDbType.Bigint,
        [("Media.Channels.ChannelSubscriptionRepository.UpdateForSystem", "Id")] = NpgsqlDbType.Bigint,
        [("Identity.UserRepository.Update", "Id")] = NpgsqlDbType.Bigint,
        [("Tasks.TaskRepository.FindTaskRunPublicIdForSystem", "taskRunId")] = NpgsqlDbType.Bigint,
    };

    public static IEnumerable<string> QueryNames => QueryRegistry.All.Keys;

    [Test]
    public void QueriesAreNotEmpty()
    {
        QueryRegistry.All.Should().NotBeEmpty();
        QueryRegistry.All.Values.Should().OnlyContain(query => !string.IsNullOrWhiteSpace(query));
    }

    [TestCaseSource(nameof(QueryNames))]
    public async Task QueryIsValid(string name)
    {
        var sql = QueryRegistry.All[name];

        await using var connection = new NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameterName in ParameterNameRegex().Matches(sql).Select(match => match.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parameter = GetType(name, parameterName) is { } type
                ? new NpgsqlParameter(parameterName, type)
                : new NpgsqlParameter { ParameterName = parameterName };

            parameter.Value = DBNull.Value;
            command.Parameters.Add(parameter);
        }

        // Preparing parses and analyses the statements without running them, so unknown tables and columns are reported.
        var prepare = async () => await command.PrepareAsync();
        await prepare.Should().NotThrowAsync($"query {name} should be valid");
    }

    private static NpgsqlDbType? GetType(string query, string parameterName)
    {
        if (QueryParameterTypes.TryGetValue((query, parameterName), out var queryType))
        {
            return queryType;
        }

        if (ParameterTypes.TryGetValue(parameterName, out var type))
        {
            return type;
        }

        return parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ? NpgsqlDbType.Uuid : null;
    }

    [GeneratedRegex(@"(?<![\w:@])@(\w+)")]
    private static partial Regex ParameterNameRegex();
}
