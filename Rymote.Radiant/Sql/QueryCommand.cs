using System.Collections.Generic;
using Dapper;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql;

public sealed class QueryCommand
{
    public string SqlText { get; }
    public DynamicParameters Parameters { get; }
    public IReadOnlyList<QueryParameter> OrderedParameters { get; }

    public QueryCommand(string sqlText, DynamicParameters parameters)
        : this(sqlText, parameters, System.Array.Empty<QueryParameter>())
    {
    }

    public QueryCommand(string sqlText, DynamicParameters parameters, IReadOnlyList<QueryParameter> orderedParameters)
    {
        SqlText = sqlText;
        Parameters = parameters;
        OrderedParameters = orderedParameters;
    }

    public CompiledQuery ToCompiledQuery() => new CompiledQuery(SqlText, OrderedParameters);

    public override string ToString()
    {
        return SqlText;
    }

    public static implicit operator string(QueryCommand queryCommand)
    {
        return queryCommand.ToString();
    }
}
