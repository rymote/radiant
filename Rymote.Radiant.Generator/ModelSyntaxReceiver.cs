using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rymote.Radiant.Generator;

public class ModelSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> ModelClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration)
        {
            if (classDeclaration.BaseList?.Types.Any(baseType =>
                    baseType.Type.ToString().StartsWith("Model<") ||
                    baseType.Type.ToString().StartsWith("Models.Model<")) == true)
            {
                ModelClasses.Add(classDeclaration);
            }
        }
    }
}