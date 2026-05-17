using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;

namespace Rymote.Radiant.Smart.Expressions;

/// <summary>
/// Translates a setter expression of the form <c>candidate =&gt; new TModel { Property = value, ... }</c>
/// (or the anonymous-new equivalent) into <c>UpdateBuilder.Set(columnName, value)</c> calls. Each
/// captured assignment value is passed through any registered <see cref="ValueConverter"/> before
/// reaching the parameter bag.
/// </summary>
public sealed class LinqToSetTranslator
{
    private static readonly IReadOnlyDictionary<Type, ValueConverter> EmptyConverters
        = new Dictionary<Type, ValueConverter>();

    private readonly IModelMetadata modelMetadata;
    private readonly IReadOnlyDictionary<Type, ValueConverter> valueConverters;

    public LinqToSetTranslator(IModelMetadata modelMetadata)
        : this(modelMetadata, EmptyConverters)
    {
    }

    public LinqToSetTranslator(IModelMetadata modelMetadata, IReadOnlyDictionary<Type, ValueConverter> valueConverters)
    {
        this.modelMetadata = modelMetadata;
        this.valueConverters = valueConverters;
    }

    public void Apply(LambdaExpression setterExpression, UpdateBuilder updateBuilder)
    {
        Expression body = setterExpression.Body;

        if (body is MemberInitExpression memberInit)
        {
            ApplyMemberAssignments(memberInit.Bindings, updateBuilder);
            return;
        }

        if (body is NewExpression anonymousNew)
        {
            ApplyAnonymousNew(anonymousNew, updateBuilder);
            return;
        }

        throw new NotSupportedException(
            "Setter expression must be a 'new { Property = value, ... }' anonymous initializer or a member-init expression like 'new TModel { Property = value }'.");
    }

    private void ApplyMemberAssignments(IReadOnlyCollection<MemberBinding> bindings, UpdateBuilder updateBuilder)
    {
        foreach (MemberBinding binding in bindings)
        {
            if (binding is not MemberAssignment memberAssignment)
                throw new NotSupportedException(
                    $"Setter expression contains an unsupported binding kind '{binding.BindingType}'. Only simple member assignments are supported.");

            string propertyName = memberAssignment.Member.Name;
            string columnName = ResolveColumnName(propertyName);
            object? capturedValue = EvaluateAsConstant(memberAssignment.Expression);
            object? convertedValue = ApplyConverterIfPresent(capturedValue);

            updateBuilder.Set(columnName, convertedValue!);
        }
    }

    private void ApplyAnonymousNew(NewExpression anonymousNew, UpdateBuilder updateBuilder)
    {
        if (anonymousNew.Members is null)
            throw new NotSupportedException("Anonymous-type setter expression has no member metadata; use a 'new T { ... }' shape instead.");

        for (int argumentIndex = 0; argumentIndex < anonymousNew.Arguments.Count; argumentIndex++)
        {
            string propertyName = anonymousNew.Members[argumentIndex].Name;
            string columnName = ResolveColumnName(propertyName);
            object? capturedValue = EvaluateAsConstant(anonymousNew.Arguments[argumentIndex]);
            object? convertedValue = ApplyConverterIfPresent(capturedValue);

            updateBuilder.Set(columnName, convertedValue!);
        }
    }

    private string ResolveColumnName(string propertyName)
    {
        IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata is null)
            throw new InvalidOperationException(
                $"Property '{propertyName}' is not registered on model '{modelMetadata.TableName}'.");

        return propertyMetadata.ColumnName;
    }

    private object? ApplyConverterIfPresent(object? value)
    {
        if (value is null) return null;
        if (valueConverters.TryGetValue(value.GetType(), out ValueConverter? converter))
            return converter.ToDatabase(value);
        return value;
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
                    $"Unsupported member type '{memberExpression.Member.GetType().Name}' when evaluating setter expression constant.")
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

        Expression<Func<object?>> lambda = Expression.Lambda<Func<object?>>(
            Expression.Convert(unwrappedNode, typeof(object)));
        return lambda.Compile()();
    }

    private static Expression UnwrapConvert(Expression expressionNode)
    {
        while (expressionNode is UnaryExpression unaryExpression
               && (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            expressionNode = unaryExpression.Operand;
        }
        return expressionNode;
    }
}
