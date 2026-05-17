using System.Globalization;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.MySql;

public sealed class MySqlParameterFormatter : IParameterFormatter
{
    public string FormatPlaceholder(int ordinal) => "@" + FormatParameterName(ordinal);

    public string FormatParameterName(int ordinal) => "p" + ordinal.ToString(CultureInfo.InvariantCulture);
}
