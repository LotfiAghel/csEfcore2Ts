using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;

namespace SyntaxWalker
{
    public class PropInf
    {
        public bool fromSuper;
        

        public PropInf(string v, TsTypeInf tsTypeInf, bool v1=false)
        {
            this.name = v;
            this.type = tsTypeInf;
            this.fromSuper = v1;
        }

        public string name { get; set; }
        public TsTypeInf type { get; set; }
    }
    public interface ITypeBlock : IBlockDespose
    {

    }
    public interface IEnumBlock : ITypeBlock
    { 
    }
    public interface IClassBlock: ITypeBlock
    {
        BlockDespose newConstructor(ITypeSymbol superClassSymbol, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps, SemanticModel sm);
        void toJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps);
        void fromJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps);
        void CreatorPolimorphic(ITypeSymbol clk, TypeDes clv, Dictionary<ITypeSymbol, TypeDes> managerMap);
        
    }


    public class EnumBlock : IEnumBlock
    {
        public List<IBlockDespose> lines { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public HashSet<ITypeSymbol> usedTypes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void addUsedType(ITypeSymbol type)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public string getFileName()
        {
            throw new NotImplementedException();
        }

        public BlockDespose newBlock(string text)
        {
            throw new NotImplementedException();
        }

        public IClassBlock newClass(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public IEnumBlock newEnum(EnumDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public IBlockDespose newFunction(string name, List<IPropertySymbol> args, string returnType, bool isAsync = false)
        {
            throw new NotImplementedException();
        }

        public IClassBlock newStruct(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public void WriteLine(string text)
        {
            
        }
    }

}