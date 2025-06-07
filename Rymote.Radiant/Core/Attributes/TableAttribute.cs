namespace Rymote.Radiant.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string Name { get; }
    public string Schema { get; set; } = "public";

    public TableAttribute(string name)
    {
        Name = name;
    }
}