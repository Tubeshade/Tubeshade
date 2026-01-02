using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;
using Dapper;
using Npgsql;
using NpgsqlTypes;
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Tubeshade.Data.Dapper;

internal sealed class SmartEnumTypeHandler<[DynamicallyAccessedMembers(All)] TEnum> : SmartEnumTypeHandler<TEnum, int>
    where TEnum : SmartEnum<TEnum, int>;

internal class SmartEnumTypeHandler<[DynamicallyAccessedMembers(All)] TEnum, TValue> : SqlMapper.TypeHandler<TEnum>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TEnum? value)
    {
        parameter.Value = value?.Name;

        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Unknown;
        }
        else
        {
            parameter.DbType = DbType.Object;
        }
    }

    /// <inheritdoc />
    public override TEnum? Parse(object value) => value switch
    {
        string name => SmartEnum<TEnum, TValue>.FromName(name),
        TValue numericValue => SmartEnum<TEnum, TValue>.FromValue(numericValue),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported enum type"),
    };
}
