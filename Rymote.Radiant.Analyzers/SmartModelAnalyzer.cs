using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rymote.Radiant.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SmartModelAnalyzer : DiagnosticAnalyzer
{
    public const string MissingTableAttributeDiagnosticId = "RR0010";
    public const string MissingPrimaryKeyDiagnosticId = "RR0011";
    public const string MultiplePrimaryKeysDiagnosticId = "RR0012";
    public const string InvalidRelationshipDiagnosticId = "RR0013";
    public const string MissingColumnAttributeDiagnosticId = "RR0014";

    private static readonly DiagnosticDescriptor MissingTableAttributeRule = new DiagnosticDescriptor(
        MissingTableAttributeDiagnosticId,
        "Missing Table attribute",
        "SmartModel class '{0}' must have a [Table] attribute",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes inheriting from SmartModel must have a Table attribute to specify the database table.");

    private static readonly DiagnosticDescriptor MissingPrimaryKeyRule = new DiagnosticDescriptor(
        MissingPrimaryKeyDiagnosticId,
        "Missing primary key",
        "SmartModel class '{0}' must have at least one property with [PrimaryKey] attribute",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every SmartModel must have a primary key property.");

    private static readonly DiagnosticDescriptor MultiplePrimaryKeysRule = new DiagnosticDescriptor(
        MultiplePrimaryKeysDiagnosticId,
        "Multiple primary keys detected",
        "SmartModel class '{0}' has multiple primary keys. Only one primary key is allowed.",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A SmartModel can only have one primary key property.");

    private static readonly DiagnosticDescriptor InvalidRelationshipRule = new DiagnosticDescriptor(
        InvalidRelationshipDiagnosticId,
        "Invalid relationship configuration",
        "Relationship property '{0}' must have a proper collection type for HasMany relationships",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "HasMany relationships must use ICollection<T>, IList<T>, or List<T> as the property type.");

    private static readonly DiagnosticDescriptor MissingColumnAttributeRule = new DiagnosticDescriptor(
        MissingColumnAttributeDiagnosticId,
        "Consider adding Column attribute",
        "Property '{0}' does not have a [Column] attribute. Column name will be auto-generated as '{1}'",
        "Design",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Properties without Column attributes will have their column names auto-generated using snake_case convention.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingTableAttributeRule, 
            MissingPrimaryKeyRule, 
            MultiplePrimaryKeysRule, 
            InvalidRelationshipRule,
            MissingColumnAttributeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        ClassDeclarationSyntax classDeclaration = (ClassDeclarationSyntax)context.Node;
        INamedTypeSymbol? classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        
        if (classSymbol == null) 
            return;

        if (classSymbol.Name == "SmartModel" && 
            classSymbol.ContainingNamespace.ToDisplayString().Contains("Rymote.Radiant.Smart"))
            return;
        
        if (classSymbol.IsAbstract)
            return;

        if (InheritsFromSmartModel(classSymbol))
            AnalyzeSmartModel(context, classDeclaration, classSymbol);
    }

    private static bool InheritsFromSmartModel(INamedTypeSymbol typeSymbol)
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

    private static void AnalyzeSmartModel(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        bool hasTableAttribute = classSymbol.GetAttributes()
            .Any(attribute => attribute.AttributeClass?.Name == "TableAttribute");

        if (!hasTableAttribute)
        {
            Diagnostic diagnostic = Diagnostic.Create(MissingTableAttributeRule, classDeclaration.Identifier.GetLocation(), classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        IPropertySymbol[] properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => property.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        IPropertySymbol[] primaryKeyProperties = properties
            .Where(property => property.GetAttributes()
                .Any(attribute => attribute.AttributeClass?.Name == "PrimaryKeyAttribute"))
            .ToArray();

        if (primaryKeyProperties.Length == 0)
        {
            Diagnostic diagnostic = Diagnostic.Create(MissingPrimaryKeyRule, classDeclaration.Identifier.GetLocation(), classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
        else if (primaryKeyProperties.Length > 1)
        {
            Diagnostic diagnostic = Diagnostic.Create(MultiplePrimaryKeysRule, classDeclaration.Identifier.GetLocation(), classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        foreach (IPropertySymbol property in properties)
        {
            AnalyzeProperty(context, property);
        }
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context, IPropertySymbol property)
    {
        AttributeData[] attributes = property.GetAttributes().ToArray();
        
        AttributeData? hasManyAttribute = attributes.FirstOrDefault(attribute => attribute.AttributeClass?.Name == "HasManyAttribute");
        if (hasManyAttribute != null)
        {
            if (!IsValidCollectionType(property.Type))
            {
                Location? location = property.Locations.FirstOrDefault();
                if (location != null)
                {
                    Diagnostic diagnostic = Diagnostic.Create(InvalidRelationshipRule, location, property.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        bool hasColumnAttribute = attributes.Any(attribute => attribute.AttributeClass?.Name == "ColumnAttribute");
        bool isRelationshipProperty = attributes.Any(attribute => 
            attribute.AttributeClass?.Name == "HasManyAttribute" ||
            attribute.AttributeClass?.Name == "HasOneAttribute" ||
            attribute.AttributeClass?.Name == "BelongsToAttribute");

        if (!hasColumnAttribute && !isRelationshipProperty && property.Type.IsValueType || property.Type.SpecialType == SpecialType.System_String)
        {
            string generatedColumnName = ConvertToSnakeCase(property.Name);
            Location? location = property.Locations.FirstOrDefault();
            if (location != null)
            {
                Diagnostic diagnostic = Diagnostic.Create(MissingColumnAttributeRule, location, property.Name, generatedColumnName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsValidCollectionType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType)
        {
            string typeName = namedType.Name;
            return typeName == "ICollection" || typeName == "IList" || typeName == "List" ||
                   typeName == "IEnumerable" || typeName == "HashSet" || typeName == "Collection";
        }
        return false;
    }

    private static string ConvertToSnakeCase(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) => 
            index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }
} 