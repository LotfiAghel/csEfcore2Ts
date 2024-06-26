using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static SyntaxWalker.Program;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.ts
{
    public struct TsTypeInf
    {
        internal bool fromSuper;
        //public ITypeSymbol type0;
        public TsTypeInf(string x)
        {
            name = x;
            //type0 = type;
        }
        public string name { get; set; }
        public bool nullable { get; set; } = false;
    }
    public class FunctionCallHead
    {
        public string Name;
        public List<Tuple<string, string>> items { get; set; } = new List<Tuple<string, string>>();

        public void AddRange(List<Tuple<string, string>> list)
        {
            items.AddRange(list);
        }
        public override string ToString()
        {
            if (items.Count == 0)
                return Name + "()";
            return Name + "(" + items.ConvertAll(l => $"{l.Item1}:{l.Item2}").Aggregate((l, r) => $"{l},{r}") + ")";
        }
    }

    public class BlockDespose : BlockDespose0
    {
       
        public BlockDespose(string name, IBlockDespose parnet, int tab):base(name, parnet, tab) 
        {
        }

     
        internal void SuperCunstrocotrCall(List<string> list)
        {
            functionCall("super", list);
        }

        public override BlockDespose newFunction(string name, List<IPropertySymbol> args, string returnType, bool isAsync = false)
        {
            var asyncS = isAsync ? "async" : "";
            var argsS = args?.ToList().ConvertAll(x => $"{x.Name}:{x.Type}").agregate();
            return newBlock($"{(isAsync ? "async" : "")} {name}({argsS}){(returnType != null ? $":{returnType}" : "")}");

        }




        public override IClassBlock newClass(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var z = new ClassBlock(class_, sm, this, tab + 1);
            lines.Add(z);
            return z;

        }

        public override IEnumBlock newEnum(EnumDeclarationSyntax class_, SemanticModel sm)
        {
            var z = new EnumBlock();// newBlock($"export enum {class_.Identifier.ToString()} ");
            lines.Add(z);
            return z;

        }

        public override IClassBlock newStruct(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var z = new ClassBlock(class_, sm, this, tab + 1);
            lines.Add(z);
            return z;
        }
    }

}
