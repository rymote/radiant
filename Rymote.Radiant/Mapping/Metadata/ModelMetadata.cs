namespace Rymote.Radiant.Mapping.Metadata;

public sealed class ModelMetadata
{
    public string TableName { get; }
    public IReadOnlyList<ColumnMetadata> Columns { get; }
    public Type ModelType { get; }

    public ModelMetadata(string tableName, Type modelType, IReadOnlyList<ColumnMetadata> columns)
    {
        TableName = tableName;
        ModelType = modelType;
        Columns = columns;
    }
}