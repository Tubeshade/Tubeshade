using System.Data;
using Dapper;
using NodaTime;

namespace Tubeshade.Data.Dapper;

internal sealed class LocalDateTypeHandler : SqlMapper.TypeHandler<LocalDate>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, LocalDate value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }

    /// <inheritdoc />
    public override LocalDate Parse(object value)
    {
        return (LocalDate)value;
    }
}
