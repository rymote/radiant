using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class ValuesClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }
    public IReadOnlyList<string> ParameterNames { get; }

    public ValuesClause(IReadOnlyList<string> columnNames, IReadOnlyList<string> parameterNames)
    {
        ColumnNames = columnNames;
        ParameterNames = parameterNames;
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
        
        
        for (int index = 0; index < ParameterNames.Count; index++)
        {
            stringBuilder.Append(SqlKeywords.PARAMETER_PREFIX).Append(ParameterNames[index]);
            if (index < ParameterNames.Count - 1) stringBuilder.Append(SqlKeywords.COMMA);
        }

        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteRaw("(");
        for (int index = 0; index < ColumnNames.Count; index++)
        {
            emitter.WriteIdentifier(ColumnNames[index]);
            if (index < ColumnNames.Count - 1)
                emitter.WriteRaw(", ");
        }

        emitter.WriteRaw(")")
            .WriteSpace()
            .WriteKeyword(emitter.Dialect.Values)
            .WriteSpace()
            .WriteRaw("(");

        for (int index = 0; index < ParameterNames.Count; index++)
        {
            emitter.WriteRaw("@").WriteRaw(ParameterNames[index]);
            if (index < ParameterNames.Count - 1) emitter.WriteRaw(", ");
        }

        emitter.WriteRaw(")");
    }
}
