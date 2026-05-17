using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class SubqueryExpression : ISqlExpression
{
    public IQueryBuilder Query { get; }
    public string? Alias { get; }

    public SubqueryExpression(IQueryBuilder query, string? alias = null)
    {
        Query = query;
        Alias = alias;
    }

    public void Accept(SqlEmitter emitter)
    {
        QueryCommand queryCommand = Query.Build(emitter.Adapter);
        emitter.WriteRaw("(").WriteRaw(queryCommand.SqlText).WriteRaw(")");

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}
