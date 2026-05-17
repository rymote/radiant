using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Loading;

public static class IncludeExtensions
{
    public static IIncludableSmartQuery<TRoot, TProperty> IncludeChain<TRoot, TProperty>(
        this ISmartQuery<TRoot> query,
        Expression<Func<TRoot, TProperty?>> navigation)
        where TRoot : class, new()
        where TProperty : class
    {
        string memberName = ExtractMemberName(navigation);
        query.Include(memberName);
        return new IncludableSmartQuery<TRoot, TProperty>(query, memberName);
    }

    public static IIncludableSmartQuery<TRoot, TProperty> IncludeChain<TRoot, TProperty>(
        this ISmartQuery<TRoot> query,
        Expression<Func<TRoot, IEnumerable<TProperty>>> navigation)
        where TRoot : class, new()
        where TProperty : class
    {
        string memberName = ExtractMemberName(navigation);
        query.Include(memberName);
        return new IncludableSmartQuery<TRoot, TProperty>(query, memberName);
    }

    private static string ExtractMemberName(LambdaExpression lambda)
    {
        Expression body = lambda.Body;
        while (body is UnaryExpression unaryExpression
               && (unaryExpression.NodeType == ExpressionType.Convert
                   || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        throw new ArgumentException(
            "IncludeChain requires a direct member access (for example: x => x.Property).", nameof(lambda));
    }
}
