using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;

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

        QueryCommand queryCommand = Query.Build(emitter.Adapter);
        emitter.WriteRaw("(").WriteRaw(queryCommand.SqlText).WriteRaw(")");

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}
