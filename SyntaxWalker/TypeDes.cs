using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;



//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
    public class TypeDes
    {
        public ITypeSymbol keyType;
        public TypeInfo type;
        public string fn;
        public bool isOld = false;
        internal List<PropertyDeclarationSyntax> filds;
        public HashSet<ITypeSymbol> usedTypes = new();
        public bool isResource { get; internal set; }
        public bool isHide { get; internal set; } = true;
    }
}
