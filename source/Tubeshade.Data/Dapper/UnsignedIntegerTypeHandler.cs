using System.Data;
using Dapper;

namespace Tubeshade.Data.Dapper;

internal sealed class UnsignedIntegerTypeHandler : SqlMapper.TypeHandler<uint?>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, uint? value)
    {
        parameter.DbType = DbType.Int32;
        parameter.Value = (int?)value;
    }

    /// <inheritdoc />
    public override uint? Parse(object? value) => value is int number ? (uint)number : null;
}
