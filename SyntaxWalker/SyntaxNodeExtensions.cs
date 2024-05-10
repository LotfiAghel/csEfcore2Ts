using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SyntaxNodeExtensions
{
    public static ITypeSymbol getBaseClass(this TypeDeclarationSyntax class_, SemanticModel sm)
    {

        if (class_.BaseList != null)
        {
            var first = true;
            foreach (var base_ in class_.BaseList.Types)
            {

                var rmp2 = sm.GetTypeInfo(base_.Type);

                if (rmp2.Type.TypeKind == TypeKind.Class)
                    return rmp2.Type;
                else
                    return null;
            }
        }
        return null;


    }
   /* public static IEnumerable<BaseTypeDeclarationSyntax>
 FindClassesDerivedOrImplementedByType(INamedTypeSymbol target, SemanticModel model)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            foreach (var type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var baseClasses = GetBaseClasses(semanticModel, type);
                if (baseClasses != null)
                    if (baseClasses.Contains(target))
                
            yield return type;
            }
        }
    }*/
    public static List<INamedTypeSymbol> GetBaseClasses(this BaseTypeDeclarationSyntax type ,SemanticModel model)
    {
        var classSymbol = model.GetDeclaredSymbol(type);
        var returnValue = new List<INamedTypeSymbol>();
        while (classSymbol.BaseType != null)
        {
            if (classSymbol.BaseType.Name == "Object")
                break;
            returnValue.Add(classSymbol.BaseType);
            //if (classSymbol.Interfaces != null)
            //    returnValue.AddRange(classSymbol.Interfaces);
            classSymbol = classSymbol.BaseType;
        }
        return returnValue;
    }
    public static List<ITypeSymbol> getBaseHirarKey(this TypeDeclarationSyntax class_, SemanticModel sm)
    {
        var res = new List<ITypeSymbol>();
        if (class_.BaseList != null)
        {
            foreach (var base_ in class_.BaseList.Types)
            {

                var rmp2 = sm.GetTypeInfo(base_.Type);

                if (rmp2.Type.TypeKind == TypeKind.Class)
                {
                    res.Add(rmp2.Type);
                    //res.Add(base_.Type);
                    //(base_.Type.SyntaxTree as TypeDeclarationSyntax).getBaseHirarKey(,sm);
                    
                        
                }
                else
                    return res;
            }
        }
        return res;


    }
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
    public static string agregate(this List<string> args)
    {
        var argsS = "";
        if (args != null && args.Count() > 0)
        {


            argsS = args.Aggregate((l, r) => $"{l},{r}");

        }
        return argsS;
    }

}