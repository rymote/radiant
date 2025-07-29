namespace Rymote.Radiant.Sql.Exceptions;

public sealed class QueryCompilationException : QueryBuilderException
{
    public string BuilderType { get; }
    public IReadOnlyList<string> MissingClauses { get; }

    public QueryCompilationException(string builderType, params string[] missingClauses)
        : base($"Cannot compile {builderType}: Missing required clause(s): {string.Join(", ", missingClauses)}. " +
               $"Ensure all required clauses are defined before calling Build().")
    {
        BuilderType = builderType;
        MissingClauses = missingClauses.ToList().AsReadOnly();
    }

    public QueryCompilationException(string builderType, string message)
        : base($"Cannot compile {builderType}: {message}")
    {
        BuilderType = builderType;
        MissingClauses = [];
    }
}