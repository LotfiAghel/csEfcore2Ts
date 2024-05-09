using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

public static class SyntaxNodeExtensions
{
    public static string? GetNamespace(this SyntaxNode syntaxNode)
    {
        return string.Join(".", syntaxNode
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(_ => _.Name)
            );
    }
    public static string GetFullName(this NullableTypeSyntax syntaxNode)
    {
        
        var z = syntaxNode.GetNamespace();
        if (z != null)
            return z + "." + syntaxNode.ToString();
        return syntaxNode.ToString();
    }
    public static string GetFullName(this TypeSyntax syntaxNode)
    {
        if(syntaxNode is PredefinedTypeSyntax aa)
        {
            return aa.GetFullName();
        }
        if (syntaxNode is NullableTypeSyntax na)
        {
            return na.GetFullName();
        }
        var z = syntaxNode.GetNamespace();
        if (z != null)
            return z + "." + syntaxNode.ToString();
        return syntaxNode.ToString();
    }
    public static string GetFullName(this PredefinedTypeSyntax syntaxNode)
    {
        return syntaxNode.ToString();
    }
    public static string GetFullName(this SyntaxNode syntaxNode)
    {
        var z=syntaxNode.GetNamespace();
        if(z!=null) 
            return z+"."+syntaxNode.ToString();
        return syntaxNode.ToString();
    }

    
    public static string GetFullName(this BaseTypeDeclarationSyntax syntaxNode)
    {
        var z = syntaxNode.GetNamespace();
        if (z != null)
            return z + "." + syntaxNode.Identifier.ToString();
        return syntaxNode.Identifier.ToString();
    }
    public static string GetFullName(this ClassDeclarationSyntax syntaxNode)
    {
        var z = syntaxNode.GetNamespace();
        if (z != null)
            return z + "." + syntaxNode.Identifier.ToString();
        return syntaxNode.Identifier.ToString();
    }
    
}