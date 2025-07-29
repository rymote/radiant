using Rymote.Radiant.Sql.Exceptions;

namespace Rymote.Radiant.Sql.Validation;

public static class QueryBuilderStateValidator
{
    public static void ValidateNotNullOrWhiteSpace(string? value, string parameterName, string? customMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(value)) return;

        string message = customMessage ?? "Value cannot be null, empty, or contain only whitespace characters";
        throw new InvalidQueryParameterException(parameterName, value, message);
    }

    public static void ValidateNotNull<T>(T? value, string parameterName, string? customMessage = null) where T : class
    {
        if (value != null) return;

        string message = customMessage ?? "Value cannot be null";
        throw new InvalidQueryParameterException(parameterName, null, message);
    }

    public static void ValidateNotEmpty<T>(IEnumerable<T> collection, string parameterName,
        string? customMessage = null)
    {
        if (collection.Any()) return;

        string message = customMessage ?? "Collection cannot be empty";
        throw new InvalidQueryParameterException(parameterName, collection, message);
    }

    public static void ValidatePositive(int value, string parameterName, string? customMessage = null)
    {
        if (value > 0) return;

        string message = customMessage ?? "Value must be greater than zero";
        throw new InvalidQueryParameterException(parameterName, value, message);
    }

    public static void ValidateNonNegative(int value, string parameterName, string? customMessage = null)
    {
        if (value >= 0) return;

        string message = customMessage ?? "Value must be greater than or equal to zero";
        throw new InvalidQueryParameterException(parameterName, value, message);
    }

    public static void ValidateBuilderState(bool condition, string builderType, string requiredOperation,
        string attemptedOperation)
    {
        if (!condition)
            throw new InvalidQueryBuilderStateException(builderType, requiredOperation, attemptedOperation);
    }

    public static void ValidateBuilderState(bool condition, string builderType, string message)
    {
        if (!condition)
            throw new InvalidQueryBuilderStateException(builderType, message);
    }

    public static void ValidateForCompilation(bool condition, string builderType, params string[] missingClauses)
    {
        if (!condition)
            throw new QueryCompilationException(builderType, missingClauses);
    }

    public static void ValidateSupportedOperation(bool isSupported, string operation, string context = "")
    {
        if (!isSupported)
            throw new UnsupportedQueryOperationException(operation, context);
    }
}