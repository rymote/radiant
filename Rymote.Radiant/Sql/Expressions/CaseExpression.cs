using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class CaseExpression : ISqlExpression
{
    private readonly List<(string Column, string Operator, object Value, object Result)> whenClauses = new();
    private object? elseResult;
    private string? alias;

    public CaseExpression When(string column, string operatorSymbol, object value, object result)
    {
        whenClauses.Add((column, operatorSymbol, value, result));
        return this;
    }

    public CaseExpression Else(object result)
    {
        elseResult = result;
        return this;
    }

    public CaseExpression As(string aliasName)
    {
        alias = aliasName;
        return this;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw("CASE");

        foreach ((string column, string operatorSymbol, object value, object result) in whenClauses)
        {
            emitter.WriteSpace().WriteRaw("WHEN").WriteSpace()
                .WriteIdentifier(column)
                .WriteSpace().WriteRaw(operatorSymbol).WriteSpace()
                .WritePlaceholderForValue(value)
                .WriteSpace().WriteRaw("THEN").WriteSpace()
                .WritePlaceholderForValue(result);
        }

        if (elseResult != null)
            emitter.WriteSpace().WriteRaw("ELSE").WriteSpace()
                .WritePlaceholderForValue(elseResult);

        emitter.WriteSpace().WriteRaw("END");

        if (!string.IsNullOrEmpty(alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(alias);
    }
}
