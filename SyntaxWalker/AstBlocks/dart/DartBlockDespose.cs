using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static SyntaxWalker.Program;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.Dart
{
   
    

    public class DartBlockDespose : BlockDespose0
    {
       
        public DartBlockDespose(string name, IBlockDespose parnet, int tab):base(name, parnet, tab) 
        {
        }

     
        internal void SuperCunstrocotrCall(List<string> list)
        {
            functionCall("super", list);
        }

        public override IBlockDespose newFunction(string name, List<IPropertySymbol> args, string returnType, bool isAsync = false)
        {
         
            var argsS = args?.ToList().ConvertAll(x => $"{x.Type} {x.Name}").agregate();
            return newBlock($"@override\n  {(isAsync ? "async" : "")} {(returnType != null ? $"{returnType}" : "")} {name}({argsS})");

        }





        public override IClassBlock newClass(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var z = new DartClassBlock(class_, sm, this, tab + 1);
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
            var z = new DartClassBlock(class_, sm, this, tab + 1);
            lines.Add(z);
            return z;
        }
    }

}
