using System.Data;
using System.Linq;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Tubeshade.Data.Dapper;

internal sealed class NullableStructArrayTypeHandler<TValue> : SqlMapper.TypeHandler<TValue?[]>
    where TValue : struct
{
    private readonly NpgsqlDbType _npgsqlDbType;

    internal NullableStructArrayTypeHandler(NpgsqlDbType npgsqlDbType)
    {
        _npgsqlDbType = npgsqlDbType;
    }

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TValue?[]? value)
    {
        parameter.Value = value;

        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Array | _npgsqlDbType;
        }
        else
        {
            parameter.DbType = DbType.Object;
        }
    }

    /// <inheritdoc />
    public override TValue?[]? Parse(object? value) => value switch
    {
        TValue?[] nullableArray => nullableArray,
        TValue[] nonNullableArray => nonNullableArray.Select(x => (TValue?)x).ToArray(),
        _ => null,
    };
}
