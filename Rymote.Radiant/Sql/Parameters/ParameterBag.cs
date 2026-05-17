using System.Collections.Generic;
using Dapper;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql.Parameters;

public sealed class ParameterBag
{
    private readonly DynamicParameters dynamicParameters = new();
    private readonly List<QueryParameter> orderedParameters = new();
    private readonly IParameterFormatter parameterFormatter;
    private int parameterIndex;

    public ParameterBag(IParameterFormatter parameterFormatter)
    {
        this.parameterFormatter = parameterFormatter;
    }

    /// <summary>
    /// Adds a value and returns its parameter name (without any placeholder prefix).
    /// </summary>
    public string Add(object? value)
    {
        int ordinal = parameterIndex++;
        string parameterName = parameterFormatter.FormatParameterName(ordinal);
        dynamicParameters.Add(parameterName, value);
        orderedParameters.Add(new QueryParameter(parameterName, value));
        return parameterName;
    }

    /// <summary>
    /// Adds a value and returns the dialect-correct placeholder text to embed directly in SQL
    /// (for example "@p0" on PostgreSQL, "$1" on a positional-only dialect).
    /// </summary>
    public string AddPlaceholder(object? value)
    {
        int ordinal = parameterIndex++;
        string parameterName = parameterFormatter.FormatParameterName(ordinal);
        dynamicParameters.Add(parameterName, value);
        orderedParameters.Add(new QueryParameter(parameterName, value));
        return parameterFormatter.FormatPlaceholder(ordinal);
    }

    public DynamicParameters ToDynamicParameters() => dynamicParameters;

    public IReadOnlyList<QueryParameter> ToOrderedParameters() => orderedParameters;
}
