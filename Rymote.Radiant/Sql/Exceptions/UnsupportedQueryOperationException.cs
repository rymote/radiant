namespace Rymote.Radiant.Sql.Exceptions;

public sealed class UnsupportedQueryOperationException : QueryBuilderException
{
    public string Operation { get; }
    public string Context { get; }

    public UnsupportedQueryOperationException(string operation, string context)
        : base($"Unsupported operation '{operation}' in context '{context}'. " +
               $"This operation is not currently supported by the query builder.")
    {
        Operation = operation;
        Context = context;
    }

    public UnsupportedQueryOperationException(string operation)
        : base($"Unsupported operation: {operation}")
    {
        Operation = operation;
        Context = string.Empty;
    }
}