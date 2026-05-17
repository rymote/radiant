using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.From;

public sealed class SubqueryFromClause : IQueryClause
{
    public IQueryBuilder Query { get; }
    public string Alias { get; }

    public SubqueryFromClause(IQueryBuilder query, string alias)
    {
        Query = query;
        Alias = alias;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteKeyword(emitter.Dialect.From).WriteSpace();

        QueryCommand queryCommand = Query.Build(emitter.Adapter);
        emitter.WriteRaw("(").WriteRaw(queryCommand.SqlText).WriteRaw(")");
        emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}
