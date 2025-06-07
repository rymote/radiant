namespace Rymote.Radiant.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class JsonColumnAttribute : Attribute
{
    public bool IsJsonB { get; set; } = true;
}