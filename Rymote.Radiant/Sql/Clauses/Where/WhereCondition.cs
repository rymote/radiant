using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereCondition : IWhereExpression
{
    public string ColumnName { get; }
    public string OperatorSymbol { get; }
    public object Value { get; }

    public WhereCondition(string columnName, string operatorSymbol, object value)
    {
        ColumnName = columnName;
        OperatorSymbol = operatorSymbol;
        Value = value;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteIdentifier(ColumnName)
            .WriteSpace().WriteRaw(OperatorSymbol).WriteSpace();

        if ((OperatorSymbol.Equals("IS", StringComparison.OrdinalIgnoreCase) ||
             OperatorSymbol.Equals("IS NOT", StringComparison.OrdinalIgnoreCase)) &&
            Value == null)
        {
            emitter.WriteKeyword(emitter.Dialect.NullLiteral);
        }
        else if (OperatorSymbol.Equals("= ANY", StringComparison.OrdinalIgnoreCase) && Value is Array)
        {
            emitter.WriteRaw("(").WritePlaceholderForValue(Value).WriteRaw(")");
        }
        else if ((OperatorSymbol.Equals("IN", StringComparison.OrdinalIgnoreCase) ||
                  OperatorSymbol.Equals("NOT IN", StringComparison.OrdinalIgnoreCase)) &&
                 Value is Array array)
        {
            emitter.WriteRaw("(");

            for (int index = 0; index < array.Length; index++)
            {
                if (index > 0)
                    emitter.WriteRaw(", ");

                emitter.WritePlaceholderForValue(array.GetValue(index));
            }

            emitter.WriteRaw(")");
        }
        else
        {
            emitter.WritePlaceholderForValue(Value);
        }
    }
}
