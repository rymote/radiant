using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class ValuesExpressionClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }
    public IReadOnlyList<ISqlExpression> Expressions { get; }

    public ValuesExpressionClause(IReadOnlyList<string> columnNames, IReadOnlyList<ISqlExpression> expressions)
    {
        ColumnNames = columnNames;
        Expressions = expressions;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);
        for (int index = 0; index < ColumnNames.Count; index++)
        {
            stringBuilder.Append(SqlKeywords.QUOTE).Append(ColumnNames[index]).Append(SqlKeywords.QUOTE);
            if (index < ColumnNames.Count - 1) 
                stringBuilder.Append(SqlKeywords.COMMA);
        }

        stringBuilder
            .Append(SqlKeywords.CLOSE_PAREN)
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.VALUES)
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.OPEN_PAREN);
        
        for (int index = 0; index < Expressions.Count; index++)
        {
            Expressions[index].AppendTo(stringBuilder);
            if (index < Expressions.Count - 1) 
                stringBuilder.Append(SqlKeywords.COMMA);
        }
        
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
    }
}