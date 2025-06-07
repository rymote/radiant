namespace Rymote.Radiant.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    public string Name { get; }
    public string Type { get; set; } = "";
    public bool IsNullable { get; set; } = true;
    public bool IsUnique { get; set; } = false;

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}