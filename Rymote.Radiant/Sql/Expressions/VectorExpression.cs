using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public enum VectorOperator
{
    L2Distance, // <->
    InnerProduct, // <#>  
    CosineDistance, // <=>
    CosineSimilarity // 1 - (vector1 <=> vector2)
}

public sealed class VectorExpression : ISqlExpression
{
    public ISqlExpression LeftVector { get; }
    public VectorOperator Operator { get; }
    public ISqlExpression RightVector { get; }

    public VectorExpression(ISqlExpression leftVector, VectorOperator vectorOperator, ISqlExpression rightVector)
    {
        LeftVector = leftVector;
        Operator = vectorOperator;
        RightVector = rightVector;
    }

    public static VectorExpression L2Distance(string column, float[] vector) =>
        L2Distance(new ColumnExpression(column), vector);

    public static VectorExpression L2Distance(ISqlExpression column, float[] vector) =>
        new(column, VectorOperator.L2Distance, new VectorLiteralExpression(vector));

    public static VectorExpression CosineSimilarity(string column, float[] vector) =>
        CosineSimilarity(new ColumnExpression(column), vector);

    public static VectorExpression CosineSimilarity(ISqlExpression column, float[] vector) =>
        new(new LiteralExpression(1), VectorOperator.CosineDistance,
            new VectorExpression(column, VectorOperator.CosineDistance,
                new VectorLiteralExpression(vector)));

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(LeftVector);
        emitter.WriteSpace().WriteRaw(GetDialectOperator(emitter)).WriteSpace();
        emitter.Emit(RightVector);
    }

    private string GetDialectOperator(SqlEmitter emitter) => Operator switch
    {
        VectorOperator.L2Distance => emitter.Dialect.Vector.L2DistanceOperator,
        VectorOperator.InnerProduct => emitter.Dialect.Vector.InnerProductOperator,
        VectorOperator.CosineDistance => emitter.Dialect.Vector.CosineDistanceOperator,
        VectorOperator.CosineSimilarity => "cosine_similarity",
        _ => throw new ArgumentOutOfRangeException()
    };
}

public sealed class VectorLiteralExpression : ISqlExpression
{
    public float[] Vector { get; }

    public VectorLiteralExpression(float[] vector) => Vector = vector;

    public void Accept(SqlEmitter emitter)
    {
        emitter.WritePlaceholderForValue(Vector);
    }
}