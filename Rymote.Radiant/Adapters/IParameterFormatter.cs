namespace Rymote.Radiant.Adapters;

public interface IParameterFormatter
{
    string FormatPlaceholder(int ordinal);
    string FormatParameterName(int ordinal);
}
