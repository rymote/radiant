using System;
using System.Globalization;
using System.Text;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlValueWriter : IValueWriter
{
    public void WriteLiteral(StringBuilder buffer, object? value)
    {
        if (value is null)
        {
            buffer.Append("NULL");
            return;
        }

        switch (value)
        {
            case bool booleanValue:
                buffer.Append(booleanValue ? "TRUE" : "FALSE");
                break;
            case string stringValue:
                buffer.Append('\'').Append(stringValue.Replace("'", "''")).Append('\'');
                break;
            case int or long or short or byte or sbyte or uint or ulong or ushort:
                buffer.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            case float floatValue:
                buffer.Append(floatValue.ToString("R", CultureInfo.InvariantCulture));
                break;
            case double doubleValue:
                buffer.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture));
                break;
            case decimal decimalValue:
                buffer.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                break;
            case DateTime dateTimeValue:
                buffer.Append('\'').Append(dateTimeValue.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                break;
            case DateTimeOffset dateTimeOffsetValue:
                buffer.Append('\'').Append(dateTimeOffsetValue.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                break;
            case Guid guidValue:
                buffer.Append('\'').Append(guidValue.ToString("D")).Append('\'').Append("::uuid");
                break;
            default:
                buffer.Append('\'').Append(value.ToString()!.Replace("'", "''")).Append('\'');
                break;
        }
    }

    public void WriteTypedLiteral(StringBuilder buffer, object? value, string databaseNativeType)
    {
        WriteLiteral(buffer, value);
        buffer.Append("::").Append(databaseNativeType);
    }

    public void WriteArrayLiteral(StringBuilder buffer, Array elements, string? elementDatabaseType)
    {
        buffer.Append("ARRAY[");
        for (int index = 0; index < elements.Length; index++)
        {
            if (index > 0) buffer.Append(", ");
            WriteLiteral(buffer, elements.GetValue(index));
        }
        buffer.Append(']');
        if (!string.IsNullOrEmpty(elementDatabaseType))
            buffer.Append("::").Append(elementDatabaseType).Append("[]");
    }
}
