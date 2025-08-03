using System;
using System.Linq;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Expressions;

public sealed class JoinExpressionVisitor<TLeft, TRight> : ExpressionVisitor 
    where TLeft : class, new()
    where TRight : class, new()
{
    private readonly IModelMetadata leftMetadata;
    private readonly IModelMetadata rightMetadata;
    
    public string? LeftColumn { get; private set; }
    public string? RightColumn { get; private set; }

    public JoinExpressionVisitor(IModelMetadata leftMetadata, IModelMetadata rightMetadata)
    {
        this.leftMetadata = leftMetadata;
        this.rightMetadata = rightMetadata;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Equal)
        {
            if (node.Left is MemberExpression leftMember && node.Right is MemberExpression rightMember)
            {
                if (leftMember.Expression?.Type == typeof(TLeft))
                {
                    LeftColumn = GetColumnName(leftMember, leftMetadata);
                }
                else if (leftMember.Expression?.Type == typeof(TRight))
                {
                    RightColumn = GetColumnName(leftMember, rightMetadata);
                }

                if (rightMember.Expression?.Type == typeof(TLeft))
                {
                    LeftColumn = GetColumnName(rightMember, leftMetadata);
                }
                else if (rightMember.Expression?.Type == typeof(TRight))
                {
                    RightColumn = GetColumnName(rightMember, rightMetadata);
                }
            }
        }

        return base.VisitBinary(node);
    }

    private string? GetColumnName(MemberExpression memberExpression, IModelMetadata metadata)
    {
        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = metadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        return propertyMetadata?.ColumnName;
    }
} 