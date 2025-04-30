namespace Rymote.Radiant.Core;

public class RadiantQueryData
{
    public RadiantOperationType OperationType { get; set; } = RadiantOperationType.None;
    
    public string? Schema { get; set; }
    public string? TargetName { get; set; }
    
    public Dictionary<string, object?> CreateValues { get; } = new Dictionary<string, object?>();
    public Dictionary<string, object?> UpdateValues { get; } = new Dictionary<string, object?>();
    
    public List<string> WhereClauses { get; } = new List<string>();
    public List<string> Columns { get; } = new List<string>();
    
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    
    public Dictionary<string, string> ColumnMapping { get; } = new Dictionary<string, string>();
    
    public Dictionary<string, object?> ExtraData { get; } = new Dictionary<string, object?>();
}