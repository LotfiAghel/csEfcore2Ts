using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks
{
    public abstract class BlockDespose0 : IBlockDespose
    {
        public string header;
        public List<IBlockDespose> lines { get; set; } = new() { };
        public int tab { get; set; } = 0;
        IBlockDespose parnet;
        public bool braket = false;
        public HashSet<ITypeSymbol> usedTypes { get; set; } = new();
        public BlockDespose0(string name, IBlockDespose parnet, int tab)
        {
            header = name;
            this.parnet = parnet;
            this.tab = tab;


        }

        public void addUsedType(ITypeSymbol type) => usedTypes.Add(type);

        public void Dispose()
        {


        }

        public virtual string getFileName()
        {
            return parnet.getFileName();
        }

        public BlockDespose newBlock(string text)
        {

            var b = new BlockDespose(text, this, tab + 1);
            lines.Add(b);
            return b;
        }
        public void WriteLine(string text)
        {
            var b = new BlockDespose(text, this, tab + 1);
            lines.Add(b);

        }



        public void functionCall(string v, List<string> list)
        {
            WriteLine($"{v}({list.agregate()})");

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

        public virtual IClassBlock newClass(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumBlock newEnum(EnumDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public virtual IClassBlock newStruct(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            throw new NotImplementedException();
        }

        public virtual IBlockDespose newFunction(string name, List<IPropertySymbol> args, string returnType, bool isAsync = false)
        {
            throw new NotImplementedException();
        }
    }

}
