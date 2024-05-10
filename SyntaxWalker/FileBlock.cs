using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
   
    public class ClassBlock : BlockDespose
    {
        
        
        public HashSet<ITypeSymbol> usedTypes = new();

        public ClassBlock(string name, BlockDespose parnet,int tab) : base(name, parnet, tab )
        {

        }

       
    }
    public class FileBlock : BlockDespose
    {
        //public IBaseWriter writer;
        public string fn;
        
        public List<string> classes=new();

        public FileBlock(string name, BlockDespose parnet, int tab) : base(name, parnet, tab )
        {
        }

        public void WriteLine(string text)
        {
            var line = new BlockDespose(text,this, tab + 1);
            line.tab = tab+1;
            lines.Add(line);
            
        }
        public BlockDespose newBlock(string text)
        {

            return new BlockDespose(text, this, tab + 1);
        }
        public virtual BlockDespose newNameSpace(string name)
        {
            return new BlockDespose($"export namespace {name}   ", this,tab+1);
        }

        public override string getFileName()
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

        public ClassBlock newClass(string text)
        {
            var z=new ClassBlock(text, this, tab + 1) ;
            lines.Add(z);
            return z;
        }
    }
  
}
