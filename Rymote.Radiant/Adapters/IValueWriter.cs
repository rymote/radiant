using System.Text;

namespace Rymote.Radiant.Adapters;

public interface IValueWriter
{
    void WriteLiteral(StringBuilder buffer, object? value);
    void WriteTypedLiteral(StringBuilder buffer, object? value, string databaseNativeType);
    void WriteArrayLiteral(StringBuilder buffer, System.Array elements, string? elementDatabaseType);
}
