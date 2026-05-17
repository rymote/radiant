using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Clauses.Where;

namespace Rymote.Radiant.Smart.Expressions;

/// <summary>
/// Translates a LINQ predicate expression tree into calls on a <see cref="SelectBuilder"/>.
/// Supported expression shapes:
///   - Binary comparisons (==, !=, &lt;, &lt;=, &gt;, &gt;=) against constants or captured variables
///   - Logical operators &amp;&amp;, ||, !
///   - String.Contains / StartsWith / EndsWith (translated to LIKE patterns)
///   - IEnumerable&lt;T&gt;.Contains (translated to IN)
///   - Boolean property access (treated as "column = TRUE")
///   - Null comparisons (rendered as IS NULL / IS NOT NULL)
/// </summary>
public sealed class LinqPredicateTranslator
{
    private readonly IModelMetadata modelMetadata;

    public LinqPredicateTranslator(IModelMetadata modelMetadata)
    {
        this.modelMetadata = modelMetadata;
    }

    public void TranslateInto(LambdaExpression predicate, SelectBuilder selectBuilder)
        => TranslateRoot(predicate.Body, selectBuilder);

    private void TranslateRoot(Expression expressionNode, SelectBuilder selectBuilder)
    {
        TranslateOperation operation = TranslateNode(expressionNode, isFirst: !selectBuilder.WhereClause.HasConditions);
        operation.ApplyToSelectBuilder(selectBuilder);
    }

    private TranslateOperation TranslateNode(Expression expressionNode, bool isFirst)
    {
        Expression unwrappedNode = UnwrapConvert(expressionNode);
        return unwrappedNode.NodeType switch
        {
            ExpressionType.AndAlso => TranslateAnd((BinaryExpression)unwrappedNode, isFirst),
            ExpressionType.OrElse => TranslateOr((BinaryExpression)unwrappedNode, isFirst),
            ExpressionType.Not => TranslateNot((UnaryExpression)unwrappedNode, isFirst),
            ExpressionType.Equal
                or ExpressionType.NotEqual
                or ExpressionType.LessThan
                or ExpressionType.LessThanOrEqual
                or ExpressionType.GreaterThan
                or ExpressionType.GreaterThanOrEqual
                => TranslateBinaryComparison((BinaryExpression)unwrappedNode, isFirst),
            ExpressionType.Call => TranslateMethodCall((MethodCallExpression)unwrappedNode, isFirst, negated: false),
            ExpressionType.MemberAccess => TranslateBooleanMemberAccess((MemberExpression)unwrappedNode, isFirst, negated: false),
            _ => throw new NotSupportedException(
                $"LinqPredicateTranslator does not yet support expression node type '{unwrappedNode.NodeType}'.")
        };
    }

    private TranslateOperation TranslateAnd(BinaryExpression node, bool isFirst)
    {
        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            void Apply(SelectBuilder target)
            {
                TranslateOperation leftOperation = TranslateNode(node.Left, isFirst: !target.WhereClause.HasConditions);
                leftOperation.ApplyToSelectBuilder(target);

                TranslateOperation rightOperation = TranslateNode(node.Right, isFirst: false);
                rightOperation.ApplyToSelectBuilder(target);
            }

            if (isFirst)
            {
                Apply(selectBuilder);
            }
            else
            {
                selectBuilder.And(group =>
                {
                    AddSubExpressionToGroup(group, node.Left, isFirst: true);
                    AddSubExpressionToGroup(group, node.Right, isFirst: false);
                });
            }
        });
    }

    private TranslateOperation TranslateOr(BinaryExpression node, bool isFirst)
    {
        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            if (isFirst)
            {
                throw new InvalidOperationException(
                    "An OR predicate cannot be the first WHERE clause; combine it with an initial AND condition.");
            }

            selectBuilder.Or(group =>
            {
                AddSubExpressionToGroup(group, node.Left, isFirst: true);
                AddSubExpressionToGroup(group, node.Right, isFirst: false);
            });
        });
    }

    private void AddSubExpressionToGroup(WhereGroup group, Expression expressionNode, bool isFirst)
    {
        Expression unwrappedNode = UnwrapConvert(expressionNode);
        switch (unwrappedNode.NodeType)
        {
            case ExpressionType.AndAlso:
                BinaryExpression andBinary = (BinaryExpression)unwrappedNode;
                group.And(subGroup =>
                {
                    AddSubExpressionToGroup(subGroup, andBinary.Left, isFirst: true);
                    AddSubExpressionToGroup(subGroup, andBinary.Right, isFirst: false);
                });
                break;
            case ExpressionType.OrElse:
                BinaryExpression orBinary = (BinaryExpression)unwrappedNode;
                group.Or(subGroup =>
                {
                    AddSubExpressionToGroup(subGroup, orBinary.Left, isFirst: true);
                    AddSubExpressionToGroup(subGroup, orBinary.Right, isFirst: false);
                });
                break;
            default:
                (string columnName, string operatorSymbol, object value) = ResolveLeafCondition(unwrappedNode);
                if (isFirst)
                    group.And(columnName, operatorSymbol, value);
                else
                    group.And(columnName, operatorSymbol, value);
                break;
        }
    }

    private (string ColumnName, string OperatorSymbol, object Value) ResolveLeafCondition(Expression expressionNode)
    {
        Expression unwrappedNode = UnwrapConvert(expressionNode);
        if (unwrappedNode is BinaryExpression binaryExpression && IsComparisonOperator(binaryExpression.NodeType))
        {
            string columnName = ResolveColumnName(binaryExpression.Left);
            object? value = EvaluateAsConstant(binaryExpression.Right);
            string operatorSymbol = ResolveComparisonOperator(binaryExpression.NodeType, value is null);
            return (columnName, operatorSymbol, value!);
        }

        if (unwrappedNode is MethodCallExpression methodCall && methodCall.Method.DeclaringType == typeof(string))
        {
            string columnName = ResolveColumnName(methodCall.Object!);
            string pattern = methodCall.Method.Name switch
            {
                nameof(string.Contains) => "%" + EvaluateAsString(methodCall.Arguments[0]) + "%",
                nameof(string.StartsWith) => EvaluateAsString(methodCall.Arguments[0]) + "%",
                nameof(string.EndsWith) => "%" + EvaluateAsString(methodCall.Arguments[0]),
                _ => throw new NotSupportedException($"String method '{methodCall.Method.Name}' is not supported inside a grouped predicate.")
            };
            return (columnName, "LIKE", pattern);
        }

        if (unwrappedNode is MemberExpression memberExpression && memberExpression.Type == typeof(bool))
        {
            string columnName = ResolveColumnName(memberExpression);
            return (columnName, "=", true);
        }

        throw new NotSupportedException($"LinqPredicateTranslator cannot translate '{unwrappedNode}' as a leaf condition.");
    }

    private TranslateOperation TranslateNot(UnaryExpression node, bool isFirst)
    {
        Expression innerExpression = UnwrapConvert(node.Operand);
        if (innerExpression is MemberExpression booleanMember && booleanMember.Type == typeof(bool))
            return TranslateBooleanMemberAccess(booleanMember, isFirst, negated: true);

        if (innerExpression is MethodCallExpression methodCall)
            return TranslateMethodCall(methodCall, isFirst, negated: true);

        throw new NotSupportedException("LinqPredicateTranslator currently supports NOT only on a boolean member or a method call (Contains/StartsWith/EndsWith).");
    }

    private TranslateOperation TranslateBinaryComparison(BinaryExpression node, bool isFirst)
    {
        string columnName = ResolveColumnName(node.Left);
        object? value = EvaluateAsConstant(node.Right);
        string operatorSymbol = ResolveComparisonOperator(node.NodeType, value is null);

        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            if (value is null)
            {
                if (operatorSymbol == "IS NULL")
                {
                    if (isFirst) selectBuilder.WhereNull(columnName);
                    else         selectBuilder.And(columnName, "IS", null!);
                }
                else
                {
                    if (isFirst) selectBuilder.WhereNotNull(columnName);
                    else         selectBuilder.And(columnName, "IS NOT", null!);
                }
                return;
            }

            if (isFirst) selectBuilder.Where(columnName, operatorSymbol, value);
            else         selectBuilder.And(columnName, operatorSymbol, value);
        });
    }

    private TranslateOperation TranslateMethodCall(MethodCallExpression node, bool isFirst, bool negated)
    {
        if (node.Method.DeclaringType == typeof(string))
            return TranslateStringMethod(node, isFirst, negated);

        if (IsEnumerableContains(node, out Expression valueExpression, out Expression collectionExpression))
            return TranslateEnumerableContains(valueExpression, collectionExpression, isFirst, negated);

        throw new NotSupportedException(
            $"Method call '{node.Method.DeclaringType?.FullName}.{node.Method.Name}' is not supported by LinqPredicateTranslator.");
    }

    private TranslateOperation TranslateStringMethod(MethodCallExpression node, bool isFirst, bool negated)
    {
        string columnName = ResolveColumnName(node.Object!);
        string argument = EvaluateAsString(node.Arguments[0]);
        string operatorSymbol = negated ? "NOT LIKE" : "LIKE";
        string pattern = node.Method.Name switch
        {
            nameof(string.Contains) => "%" + argument + "%",
            nameof(string.StartsWith) => argument + "%",
            nameof(string.EndsWith) => "%" + argument,
            _ => throw new NotSupportedException($"String method '{node.Method.Name}' is not supported.")
        };

        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            if (isFirst) selectBuilder.Where(columnName, operatorSymbol, pattern);
            else         selectBuilder.And(columnName, operatorSymbol, pattern);
        });
    }

    private TranslateOperation TranslateEnumerableContains(Expression valueExpression, Expression collectionExpression, bool isFirst, bool negated)
    {
        string columnName = ResolveColumnName(valueExpression);
        object? collectionValue = EvaluateAsConstant(collectionExpression);
        if (collectionValue is null)
            throw new InvalidOperationException("Cannot translate Contains on a null collection.");

        string operatorSymbol = negated ? "NOT IN" : "IN";
        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            if (isFirst) selectBuilder.Where(columnName, operatorSymbol, collectionValue);
            else         selectBuilder.And(columnName, operatorSymbol, collectionValue);
        });
    }

    private TranslateOperation TranslateBooleanMemberAccess(MemberExpression node, bool isFirst, bool negated)
    {
        string columnName = ResolveColumnName(node);
        object value = !negated;
        return TranslateOperation.ApplyAction(selectBuilder =>
        {
            if (isFirst) selectBuilder.Where(columnName, "=", value);
            else         selectBuilder.And(columnName, "=", value);
        });
    }

    private static bool IsEnumerableContains(MethodCallExpression node, out Expression valueExpression, out Expression collectionExpression)
    {
        if (node.Method.Name == nameof(Enumerable.Contains) && node.Arguments.Count == 2)
        {
            collectionExpression = node.Arguments[0];
            valueExpression = node.Arguments[1];
            return true;
        }

        if (node.Method.Name == nameof(IList.Contains) && node.Object is not null && node.Arguments.Count == 1)
        {
            collectionExpression = node.Object;
            valueExpression = node.Arguments[0];
            return true;
        }

        valueExpression = null!;
        collectionExpression = null!;
        return false;
    }

    private string ResolveColumnName(Expression expressionNode)
    {
        Expression unwrappedNode = UnwrapConvert(expressionNode);
        if (unwrappedNode is not MemberExpression memberExpression)
            throw new NotSupportedException(
                $"LinqPredicateTranslator could not resolve a column name from expression '{expressionNode}'. Only direct property access is currently supported.");

        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata is null)
            throw new InvalidOperationException(
                $"Property '{propertyName}' is not registered on model '{modelMetadata.TableName}'.");

        return propertyMetadata.ColumnName;
    }

    private static bool IsComparisonOperator(ExpressionType nodeType)
        => nodeType is ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual
            or ExpressionType.GreaterThan
            or ExpressionType.GreaterThanOrEqual;

    private static string ResolveComparisonOperator(ExpressionType nodeType, bool valueIsNull) => nodeType switch
    {
        ExpressionType.Equal => valueIsNull ? "IS NULL" : "=",
        ExpressionType.NotEqual => valueIsNull ? "IS NOT NULL" : "<>",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        _ => throw new NotSupportedException($"Operator '{nodeType}' is not a comparison operator.")
    };

    private static Expression UnwrapConvert(Expression expressionNode)
    {
        while (expressionNode is UnaryExpression unaryExpression
               && (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            expressionNode = unaryExpression.Operand;
        }
        return expressionNode;
    }

    private static object? EvaluateAsConstant(Expression expressionNode)
    {
        Expression unwrappedNode = UnwrapConvert(expressionNode);
        if (unwrappedNode is ConstantExpression constantExpression)
            return constantExpression.Value;

        if (unwrappedNode is MemberExpression memberExpression)
        {
            object? targetInstance = memberExpression.Expression is null
                ? null
                : EvaluateAsConstant(memberExpression.Expression);

            return memberExpression.Member switch
            {
                System.Reflection.FieldInfo fieldInfo => fieldInfo.GetValue(targetInstance),
                System.Reflection.PropertyInfo propertyInfo => propertyInfo.GetValue(targetInstance),
                _ => throw new NotSupportedException(
                    $"Unsupported member type '{memberExpression.Member.GetType().Name}' when evaluating expression constant.")
            };
        }

        if (unwrappedNode is MethodCallExpression methodCall)
        {
            bool isImplicitConversion = methodCall.Method.IsSpecialName
                && (methodCall.Method.Name == "op_Implicit" || methodCall.Method.Name == "op_Explicit")
                && methodCall.Arguments.Count == 1
                && methodCall.Object is null;

            if (isImplicitConversion)
                return EvaluateAsConstant(methodCall.Arguments[0]);

            object? targetInstance = methodCall.Object is null
                ? null
                : EvaluateAsConstant(methodCall.Object);
            object?[] arguments = new object?[methodCall.Arguments.Count];
            for (int argumentIndex = 0; argumentIndex < methodCall.Arguments.Count; argumentIndex++)
                arguments[argumentIndex] = EvaluateAsConstant(methodCall.Arguments[argumentIndex]);
            return methodCall.Method.Invoke(targetInstance, arguments);
        }

        if (unwrappedNode is NewArrayExpression newArray)
        {
            Array result = Array.CreateInstance(newArray.Type.GetElementType()!, newArray.Expressions.Count);
            for (int elementIndex = 0; elementIndex < newArray.Expressions.Count; elementIndex++)
                result.SetValue(EvaluateAsConstant(newArray.Expressions[elementIndex]), elementIndex);
            return result;
        }

        throw new NotSupportedException(
            $"LinqPredicateTranslator cannot evaluate expression '{unwrappedNode}' (node type '{unwrappedNode.NodeType}', CLR type '{unwrappedNode.GetType().Name}') as a constant.");
    }

    private static string EvaluateAsString(Expression expressionNode)
    {
        object? value = EvaluateAsConstant(expressionNode);
        if (value is not string stringValue)
            throw new InvalidOperationException("Expected a string argument for the LINQ method call.");
        return stringValue;
    }

    private readonly struct TranslateOperation
    {
        private readonly Action<SelectBuilder> action;

        private TranslateOperation(Action<SelectBuilder> action) { this.action = action; }

        public static TranslateOperation ApplyAction(Action<SelectBuilder> action) => new(action);

        public void ApplyToSelectBuilder(SelectBuilder selectBuilder) => action(selectBuilder);
    }
}
