using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;



//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
    public class TypeDes
    {
        public ITypeSymbol keyType;
        public string keyTypeName;
        public TypeInfo type;
        public bool isNonAbstractClass = false;
        public string fn;
        public bool isOld = false;
        internal List<PropertyDeclarationSyntax> filds;
        public HashSet<ITypeSymbol> usedTypes = new();
        public ITypeBlock block;
        public TypeDeclarationSyntax syntax;
        public SemanticModel sm;
        public bool isResource { get; internal set; }
        public bool isHide { get; internal set; } = true;
        public string context { get; internal set; } = null;
        public bool isPolimorphicBase { get; set; } = false;
        public bool isClientCreatable { get; set; } = false;

    }
}
