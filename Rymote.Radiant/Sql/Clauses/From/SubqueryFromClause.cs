using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.FROM).Append(SqlKeywords.SPACE);
        
        QueryCommand queryCommand = Query.Build();
        stringBuilder.Append(SqlKeywords.OPEN_PAREN).Append(queryCommand).Append(SqlKeywords.CLOSE_PAREN);
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE);
        stringBuilder.Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
        
        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            parameterBag.Add(value);
        }
    }
}