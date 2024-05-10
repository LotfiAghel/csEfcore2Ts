using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
    public class LineItem
    {

    }
    public class Line: LineItem
    {
        public string txt;
        public override string ToString()
        {
            return txt;
        }
    }
    public class FileBlock : IBlockDespose
    {
        //public IBaseWriter writer;
        public string fn;
        public int tab = 0;
        public List<string> lines = new() { ""};
        public HashSet<ITypeSymbol> usedTypes = new();
        public List<string> classes=new();

        public void WriteLine(string text)
        {
            for (int i = 0; i < tab; ++i)
                lines[lines.Count-1]+="\t";
            lines[lines.Count - 1] += text;
            lines.Add("");
        }
        public BlockDespose newBlock(string text)
        {

            return new BlockDespose(text, this);
        }
        public virtual BlockDespose newNameSpace(string name)
        {
            return new BlockDespose($"export namespace {name}   ", this);
        }

        public string getFileName()
        {
            return fn;
        }

        public void addUsedType(ITypeSymbol type)
        {
            usedTypes.Add(type);    
        }

        public BlockDespose newFunction(string name, List<Tuple<string, string>> args, string returnType, bool isAsync = false)
        {
            var asyncS = isAsync ? "async" : "";
            var argsS = "";
            if (args != null && args.Count() > 0)
            {


                argsS = args.ToList().ConvertAll(x => $"{x.Item1}:{x.Item2}").Aggregate((l, r) => $"{l},{r}");

            }
            return newBlock($"{(isAsync ? "async" : "")} {name}({argsS}):{returnType}");
        }
    }
    
}
