using System.Data;
using Dapper;
using NodaTime;

namespace Tubeshade.Data.Dapper;

internal sealed class NullableInstantTypeHandler : SqlMapper.TypeHandler<Instant?>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Instant? value) => parameter.Value = value;

    /// <inheritdoc />
    public override Instant? Parse(object value) => (Instant?)value;
}
