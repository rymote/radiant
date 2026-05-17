using System;
using System.Globalization;
using System.Text;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.Sqlite;

public sealed class SqliteValueWriter : IValueWriter
{
    public void WriteLiteral(StringBuilder buffer, object? value)
    {
        if (value is null) { buffer.Append("NULL"); return; }
        switch (value)
        {
            case bool booleanValue: buffer.Append(booleanValue ? "1" : "0"); break;
            case string stringValue: buffer.Append('\'').Append(stringValue.Replace("'", "''")).Append('\''); break;
            case int or long or short or byte or sbyte or uint or ulong or ushort:
                buffer.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
            case float floatValue: buffer.Append(floatValue.ToString("R", CultureInfo.InvariantCulture)); break;
            case double doubleValue: buffer.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture)); break;
            case decimal decimalValue: buffer.Append(decimalValue.ToString(CultureInfo.InvariantCulture)); break;
            case DateTime dateTimeValue:
                buffer.Append('\'').Append(dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append('\''); break;
            case Guid guidValue:
                buffer.Append('\'').Append(guidValue.ToString("D")).Append('\''); break;
            default:
                buffer.Append('\'').Append(value.ToString()!.Replace("'", "''")).Append('\''); break;
        }
    }

    public void WriteTypedLiteral(StringBuilder buffer, object? value, string databaseNativeType)
    {
        // SQLite is dynamically typed; ignore the requested native type and write the value plainly.
        WriteLiteral(buffer, value);
    }

    public void WriteArrayLiteral(StringBuilder buffer, Array elements, string? elementDatabaseType)
        => throw new NotSupportedException("SQLite has no array column type.");
}
