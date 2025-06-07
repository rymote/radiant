namespace Rymote.Radiant.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class IndexAttribute : Attribute
{
    public string Name { get; set; } = "";
    public bool IsUnique { get; set; } = false;
    public bool IsConcurrent { get; set; } = true;
}