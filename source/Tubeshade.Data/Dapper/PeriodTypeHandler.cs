using System.Data;
using Dapper;
using NodaTime;
using Npgsql;
using NpgsqlTypes;

namespace Tubeshade.Data.Dapper;

internal sealed class PeriodTypeHandler : SqlMapper.TypeHandler<Period>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Period? value)
    {
        parameter.Value = value;

        if (parameter is NpgsqlParameter npgsqlParameter)
        {
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Interval;
        }
        else
        {
            parameter.DbType = DbType.Object;
        }
    }

    /// <inheritdoc />
    public override Period Parse(object value) => (Period)value;
}
