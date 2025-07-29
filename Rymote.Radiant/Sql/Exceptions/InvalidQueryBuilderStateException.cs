namespace Rymote.Radiant.Sql.Exceptions;

public sealed class InvalidQueryBuilderStateException : QueryBuilderException
{
    public string BuilderType { get; }
    public string RequiredOperation { get; }
    public string AttemptedOperation { get; }

    public InvalidQueryBuilderStateException(string builderType, string requiredOperation, string attemptedOperation)
        : base($"Invalid {builderType} state: Cannot perform '{attemptedOperation}' before '{requiredOperation}'. " +
               $"Please call {requiredOperation}() first.")
    {
        BuilderType = builderType;
        RequiredOperation = requiredOperation;
        AttemptedOperation = attemptedOperation;
    }

    public InvalidQueryBuilderStateException(string builderType, string message)
        : base($"Invalid {builderType} state: {message}")
    {
        BuilderType = builderType;
        RequiredOperation = string.Empty;
        AttemptedOperation = string.Empty;
    }
}