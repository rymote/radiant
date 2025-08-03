using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Expressions;

public sealed class WhereExpressionVisitor : ExpressionVisitor
{
    private readonly IModelMetadata modelMetadata;
    public List<(string ColumnName, string OperatorSymbol, object Value)> Conditions { get; }

    public WhereExpressionVisitor(IModelMetadata modelMetadata)
    {
        this.modelMetadata = modelMetadata;
        this.Conditions = new List<(string ColumnName, string OperatorSymbol, object Value)>();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (IsComparisonOperator(node.NodeType))
        {
            string? columnName = GetColumnName(node.Left);
            object? value = GetValue(node.Right);

            if (columnName != null && value != null)
            {
                string operatorSymbol = GetOperatorSymbol(node.NodeType);
                Conditions.Add((columnName, operatorSymbol, value));
            }
        }

        return base.VisitBinary(node);
    }

    private bool IsComparisonOperator(ExpressionType nodeType)
    {
        return nodeType == ExpressionType.Equal ||
               nodeType == ExpressionType.NotEqual ||
               nodeType == ExpressionType.GreaterThan ||
               nodeType == ExpressionType.GreaterThanOrEqual ||
               nodeType == ExpressionType.LessThan ||
               nodeType == ExpressionType.LessThanOrEqual;
    }

    private string GetOperatorSymbol(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported")
        };
    }

    private string? GetColumnName(Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            string propertyName = memberExpression.Member.Name;
            IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
                .FirstOrDefault(property => property.PropertyName == propertyName);

            return propertyMetadata?.ColumnName;
        }

        return null;
    }

    private object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }

        if (expression is MemberExpression memberExpression)
        {
            Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(
                Expression.Convert(memberExpression, typeof(object)));
            Func<object> compiledLambda = lambda.Compile();
            return compiledLambda();
        }

        return null;
    }
}