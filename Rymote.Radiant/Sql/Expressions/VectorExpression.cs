using System.Text;
using Rymote.Radiant.Sql.Dialects;

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

    public void AppendTo(StringBuilder stringBuilder)
    {
        LeftVector.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorSymbol()).Append(SqlKeywords.SPACE);
        RightVector.AppendTo(stringBuilder);
    }

    private string GetOperatorSymbol() => Operator switch
    {
        VectorOperator.L2Distance => SqlKeywords.VECTOR_L2_DISTANCE,
        VectorOperator.InnerProduct => SqlKeywords.VECTOR_INNER_PRODUCT,
        VectorOperator.CosineDistance => SqlKeywords.VECTOR_COSINE_DISTANCE,
        VectorOperator.CosineSimilarity => SqlKeywords.VECTOR_COSINE_SIMILARITY
    };

    public static VectorExpression L2Distance(string column, float[] vector) =>
        new(new ColumnExpression(column), VectorOperator.L2Distance, new VectorLiteralExpression(vector));

    public static VectorExpression CosineSimilarity(string column, float[] vector) =>
        new(new LiteralExpression(1), VectorOperator.CosineDistance,
            new VectorExpression(new ColumnExpression(column), VectorOperator.CosineDistance,
                new VectorLiteralExpression(vector)));
}

public sealed class VectorLiteralExpression : ISqlExpression
{
    public float[] Vector { get; }

    public VectorLiteralExpression(float[] vector) => Vector = vector;

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.SINGLE_QUOTE)
            .Append(SqlKeywords.OPEN_BRACKET)
            .Append(string.Join(SqlKeywords.COMMA.Trim(), Vector))
            .Append(SqlKeywords.CLOSE_BRACKET)
            .Append(SqlKeywords.SINGLE_QUOTE);
    }
}