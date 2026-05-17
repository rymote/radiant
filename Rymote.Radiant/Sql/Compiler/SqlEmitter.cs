using System.Text;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Sql.Clauses;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Compiler;

public sealed class SqlEmitter
{
    public IDatabaseAdapter Adapter { get; }
    public ISqlDialect Dialect => Adapter.Dialect;
    public IIdentifierQuoter Quoter => Adapter.IdentifierQuoter;
    public IParameterFormatter ParameterFormatter => Adapter.ParameterFormatter;
    public IValueWriter ValueWriter => Adapter.ValueWriter;
    public StringBuilder Buffer { get; }
    public ParameterBag Parameters { get; }

    public SqlEmitter(IDatabaseAdapter adapter, StringBuilder buffer, ParameterBag parameters)
    {
        Adapter = adapter;
        Buffer = buffer;
        Parameters = parameters;
    }

    public void Emit(IQueryClause clause) => clause.Accept(this);
    public void Emit(IWhereExpression whereExpression) => whereExpression.Accept(this);
    public void Emit(ISqlExpression expression) => expression.Accept(this);

    public SqlEmitter WriteRaw(string text)
    {
        Buffer.Append(text);
        return this;
    }

    public SqlEmitter WriteSpace()
    {
        Buffer.Append(' ');
        return this;
    }

    public SqlEmitter WriteIdentifier(string identifier)
    {
        Buffer.Append(Quoter.QuoteIdentifier(identifier));
        return this;
    }

    public SqlEmitter WriteQualifiedName(string? schemaName, string objectName)
    {
        Buffer.Append(Quoter.QuoteQualifiedName(schemaName, objectName));
        return this;
    }

    public SqlEmitter WritePlaceholderForValue(object? value)
    {
        Buffer.Append(Parameters.AddPlaceholder(value));
        return this;
    }

    public SqlEmitter WriteKeyword(string dialectKeyword)
    {
        Buffer.Append(dialectKeyword);
        return this;
    }
}
