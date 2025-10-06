using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace SyntaxWalker.AstBlocks
{
    public class PropInf
    {
        public bool fromSuper;


        public PropInf(string v, TsTypeInf tsTypeInf, bool v1 = false)
        {
            name = v;
            type = tsTypeInf;
            fromSuper = v1;
        }

        public string name { get; set; }
        public TsTypeInf type { get; set; }
    }
    public interface ITypeBlock : IBlockDespose
    {

    }
    public interface IEnumBlock : ITypeBlock
    {
        void addField(EnumMemberDeclarationSyntax mem, int rmp2);
    }
    public interface IClassBlock : ITypeBlock
    {
        BlockDespose newConstructor(ITypeSymbol superClassSymbol, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps, SemanticModel sm);
        void toJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<PropertyDeclarationSyntax> flatProps);
        void fromJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<PropertyDeclarationSyntax> flatProps);
        void CreatorPolimorphic(ITypeSymbol clk, TypeDes clv, Dictionary<ITypeSymbol, TypeDes> managerMap);
        void addField(PropertyDeclarationSyntax f, ITypeSymbol type, SemanticModel sm);
        void addFField(string name,string type,bool nullable=false,string defultValue=null);
    }


    public class EnumBlock : BlockDespose,IEnumBlock
    {
        public EnumBlock(string name, IBlockDespose parnet, int tab) : base(name, parnet, tab)
        {
        }

        public void addField(EnumMemberDeclarationSyntax mem, int rmp2)
        {
            WriteLine($"{mem.Identifier} = {rmp2},");
        }

        public override string ToString()
        {
            var txt = "";
            for (var i = 0; i < tab; ++i)
                txt += "\t";
            txt += header;
            if (lines.Count > 0 || braket)
            {
                txt += "{\n";
            }
            foreach (var i in lines)
                txt += $"{i.ToString()}\n";
            if (lines.Count > 0 || braket)
            {
                for (var i = 0; i < tab; ++i)
                    txt += "\t";
                txt += "}\n";
            }
            txt += "\n";
            return txt;
        }
    }

}