using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.SetOperation;

public sealed class SetOperationClause : IQueryClause
{
    public SetOperationType OperationType { get; }
    public IQueryBuilder Query { get; }

    public SetOperationClause(SetOperationType operationType, IQueryBuilder query)
    {
        OperationType = operationType;
        Query = query;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        string operationText = OperationType switch
        {
            SetOperationType.Union => SqlKeywords.UNION,
            SetOperationType.UnionAll => SqlKeywords.UNION_ALL,
            SetOperationType.Intersect => SqlKeywords.INTERSECT,
            SetOperationType.Except => SqlKeywords.EXCEPT,
            _ => throw new ArgumentOutOfRangeException()
        };

        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(operationText)
            .Append(SqlKeywords.SPACE);

        QueryCommand queryCommand = Query.Build();
        stringBuilder.Append(SqlKeywords.OPEN_PAREN).Append(queryCommand).Append(SqlKeywords.CLOSE_PAREN);

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            parameterBag.Add(value);
        }
    }

    public void Accept(SqlEmitter emitter)
    {
        string operationText = OperationType switch
        {
            SetOperationType.Union => "UNION",
            SetOperationType.UnionAll => "UNION ALL",
            SetOperationType.Intersect => "INTERSECT",
            SetOperationType.Except => "EXCEPT",
            _ => throw new ArgumentOutOfRangeException()
        };

        emitter.WriteSpace().WriteRaw(operationText).WriteSpace();

        QueryCommand queryCommand = Query.Build();
        emitter.WriteRaw("(").WriteRaw(queryCommand.SqlText).WriteRaw(")");

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}