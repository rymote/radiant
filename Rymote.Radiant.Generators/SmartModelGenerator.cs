using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Rymote.Radiant.Generators;

[Generator]
public sealed class SmartModelGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SmartModelSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SmartModelSyntaxReceiver receiver))
            return;

        foreach (ClassDeclarationSyntax classDeclaration in receiver.SmartModelClasses)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            
            if (classSymbol == null) continue;

            string generatedCode = GenerateHelperMethods(classSymbol);
            string fileName = $"{classSymbol.Name}.SmartHelpers.g.cs";
            
            context.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));
        }
    }

    private string GenerateHelperMethods(INamedTypeSymbol classSymbol)
    {
        StringBuilder sourceBuilder = new StringBuilder();
        string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        string className = classSymbol.Name;

        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine($"namespace {namespaceName};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"public static class {className}SmartQueryExtensions");
        sourceBuilder.AppendLine("{");

        IPropertySymbol[] properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => property.DeclaredAccessibility == Accessibility.Public)
            .Where(property => !IsRelationshipProperty(property))
            .ToArray();

        foreach (IPropertySymbol property in properties)
        {
            GeneratePropertyHelperMethods(sourceBuilder, className, property);
        }

        sourceBuilder.AppendLine("}");
        return sourceBuilder.ToString();
    }

    private void GeneratePropertyHelperMethods(StringBuilder sourceBuilder, string className, IPropertySymbol property)
    {
        string propertyName = property.Name;
        string columnName = GetColumnName(property);
        ITypeSymbol propertyType = property.Type;

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {GetFullTypeName(propertyType)} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"=\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Not(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {GetFullTypeName(propertyType)} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"!=\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        if (IsNumericType(propertyType))
        {
            GenerateNumericHelperMethods(sourceBuilder, className, propertyName, columnName, propertyType);
        }
        else if (IsDateTimeType(propertyType))
        {
            GenerateDateTimeHelperMethods(sourceBuilder, className, propertyName, columnName, propertyType);
        }
        else if (propertyType.SpecialType == SpecialType.System_String)
        {
            GenerateStringHelperMethods(sourceBuilder, className, propertyName, columnName);
        }
        else if (IsBooleanType(propertyType))
        {
            GenerateBooleanHelperMethods(sourceBuilder, className, propertyName, columnName);
        }

        if (IsNullableType(propertyType))
        {
            GenerateNullableHelperMethods(sourceBuilder, className, propertyName, columnName);
        }
    }

    private void GenerateNumericHelperMethods(StringBuilder sourceBuilder, string className, string propertyName, string columnName, ITypeSymbol propertyType)
    {
        string typeName = GetFullTypeName(propertyType);

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}GreaterThan(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}GreaterThanOrEqual(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}LessThan(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"<\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}LessThanOrEqual(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"<=\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Between(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} minValue,");
        sourceBuilder.AppendLine($"        {typeName} maxValue)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", minValue).Where(\"{columnName}\", \"<=\", maxValue);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private void GenerateDateTimeHelperMethods(StringBuilder sourceBuilder, string className, string propertyName, string columnName, ITypeSymbol propertyType)
    {
        string typeName = GetFullTypeName(propertyType);

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}After(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Before(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"<\", value);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Between(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        {typeName} startDate,");
        sourceBuilder.AppendLine($"        {typeName} endDate)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", startDate).Where(\"{columnName}\", \"<=\", endDate);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Today(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        global::System.DateTime today = global::System.DateTime.UtcNow.Date;");
        sourceBuilder.AppendLine($"        global::System.DateTime tomorrow = today.AddDays(1);");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", today).Where(\"{columnName}\", \"<\", tomorrow);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}ThisWeek(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        global::System.DateTime today = global::System.DateTime.UtcNow.Date;");
        sourceBuilder.AppendLine($"        int daysUntilMonday = ((int)today.DayOfWeek - (int)global::System.DayOfWeek.Monday + 7) % 7;");
        sourceBuilder.AppendLine($"        global::System.DateTime weekStart = today.AddDays(-daysUntilMonday);");
        sourceBuilder.AppendLine($"        global::System.DateTime weekEnd = weekStart.AddDays(7);");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", weekStart).Where(\"{columnName}\", \"<\", weekEnd);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}ThisMonth(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        global::System.DateTime today = global::System.DateTime.UtcNow;");
        sourceBuilder.AppendLine($"        global::System.DateTime monthStart = new global::System.DateTime(today.Year, today.Month, 1);");
        sourceBuilder.AppendLine($"        global::System.DateTime monthEnd = monthStart.AddMonths(1);");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \">=\", monthStart).Where(\"{columnName}\", \"<\", monthEnd);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private void GenerateStringHelperMethods(StringBuilder sourceBuilder, string className, string propertyName, string columnName)
    {
        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Like(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        string pattern)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"LIKE\", pattern);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}Contains(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        string value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"LIKE\", $\"%{{value}}%\");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}StartsWith(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        string value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"LIKE\", $\"{{value}}%\");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}EndsWith(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query,");
        sourceBuilder.AppendLine($"        string value)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"LIKE\", $\"%{{value}}\");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private void GenerateBooleanHelperMethods(StringBuilder sourceBuilder, string className, string propertyName, string columnName)
    {
        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}IsTrue(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"=\", true);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}IsFalse(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.Where(\"{columnName}\", \"=\", false);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private void GenerateNullableHelperMethods(StringBuilder sourceBuilder, string className, string propertyName, string columnName)
    {
        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}IsNull(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.WhereNull(\"{columnName}\");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    public static global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> Where{propertyName}IsNotNull(");
        sourceBuilder.AppendLine($"        this global::Rymote.Radiant.Smart.Query.ISmartQuery<{className}> query)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        return query.WhereNotNull(\"{columnName}\");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private string GetColumnName(IPropertySymbol property)
    {
        AttributeData? columnAttribute = property.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.Name == "ColumnAttribute");

        if (columnAttribute != null && columnAttribute.ConstructorArguments.Length > 0)
        {
            object? value = columnAttribute.ConstructorArguments[0].Value;
            if (value is string columnName)
            {
                return columnName;
            }
        }

        return ConvertToSnakeCase(property.Name);
    }

    private string ConvertToSnakeCase(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) => 
            index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }

    private bool IsRelationshipProperty(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == "HasManyAttribute" ||
            attribute.AttributeClass?.Name == "HasOneAttribute" ||
            attribute.AttributeClass?.Name == "BelongsToAttribute");
    }

    private bool IsNumericType(ITypeSymbol type)
    {
        ITypeSymbol underlyingType = GetUnderlyingType(type);
        return underlyingType.SpecialType == SpecialType.System_Int16 ||
               underlyingType.SpecialType == SpecialType.System_Int32 ||
               underlyingType.SpecialType == SpecialType.System_Int64 ||
               underlyingType.SpecialType == SpecialType.System_Decimal ||
               underlyingType.SpecialType == SpecialType.System_Single ||
               underlyingType.SpecialType == SpecialType.System_Double;
    }

    private bool IsDateTimeType(ITypeSymbol type)
    {
        ITypeSymbol underlyingType = GetUnderlyingType(type);
        return underlyingType.Name == "DateTime" || underlyingType.Name == "DateTimeOffset" || 
               underlyingType.Name == "DateOnly" || underlyingType.Name == "TimeOnly";
    }

    private bool IsBooleanType(ITypeSymbol type)
    {
        ITypeSymbol underlyingType = GetUnderlyingType(type);
        return underlyingType.SpecialType == SpecialType.System_Boolean;
    }

    private bool IsNullableType(ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated ||
               (type is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
    }

    private ITypeSymbol GetUnderlyingType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }

    private string GetFullTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private sealed class SmartModelSyntaxReceiver : ISyntaxContextReceiver
    {
        public List<ClassDeclarationSyntax> SmartModelClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclaration)
            {
                INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
                if (classSymbol != null && InheritsFromSmartModel(classSymbol))
                {
                    SmartModelClasses.Add(classDeclaration);
                }
            }
        }

        private bool InheritsFromSmartModel(INamedTypeSymbol typeSymbol)
        {
            INamedTypeSymbol? baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "SmartModel" && baseType.ContainingNamespace.ToDisplayString().Contains("Rymote.Radiant.Smart"))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
    }
} 