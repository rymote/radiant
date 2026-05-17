using System.Globalization;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlParameterFormatter : IParameterFormatter
{
    public string FormatPlaceholder(int ordinal) => "@" + FormatParameterName(ordinal);

    public string FormatParameterName(int ordinal) => "p" + ordinal.ToString(CultureInfo.InvariantCulture);
}
