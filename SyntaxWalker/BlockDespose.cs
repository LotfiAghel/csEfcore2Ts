using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static SyntaxWalker.Program;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{

    public class FunctionCallHead
    {
        public string Name;
        public List<Tuple<string, string>> items{ get; set; }= new List<Tuple<string, string>>();

        public void AddRange(List<Tuple<string, string>> list)
        {
            items.AddRange(list);
        }
        public override string ToString()
        {
            if (items.Count == 0)
                return Name+"()";
            return Name+"("+ items.ConvertAll(l => $"{l.Item1}:{l.Item2}").Aggregate((l, r) => $"{l},{r}") +")";
        }
    }
    public class BlockDespose : IDisposable, IBlockDespose
        {
            FileBlock parnet;
            public BlockDespose(string name, FileBlock parnet)
            {
                this.parnet = parnet;
                parnet.WriteLine($"{name} {{");
                parnet.tab++;

            }

        public void addUsedType(ITypeSymbol type) => parnet.addUsedType(type);

        public void Dispose()
            {
                parnet.tab--;
                parnet.WriteLine("}");

            }

        public string getFileName()
        {
            return parnet.getFileName();
        }

        public BlockDespose newBlock(string text)
            {

                return new BlockDespose(text, this.parnet);
            }
            public void WriteLine(string text)
            {
                parnet.WriteLine(text);
            }

        public BlockDespose newConstructor(List<Tuple<string, string>> args)
        {
            
            var argsS = args.ToList().ConvertAll(x => $"{x.Item1}:{x.Item2}").agregate();
            return newBlock($"constructor(args:{{ {argsS} }})");

            //return newFunction( "constructor" , args,null,false);
        }

        public void functionCall(string v, List<string> list)
        {
            WriteLine($"{v}({list.agregate()})");
            
        }

        internal void SuperCunstrocotrCall(List<string> list)
        {
            functionCall("super", list);
        }

        public BlockDespose newFunction(string name, List<Tuple<string, string>> args, string returnType, bool isAsync = false)
        {
            var asyncS = isAsync ? "async" : "";
            var argsS = args?.ToList().ConvertAll(x => $"{x.Item1}:{x.Item2}").agregate();
            return newBlock($"{(isAsync ? "async" : "")} {name}({argsS}){(returnType!=null? $":{returnType}" : "")}");
            
        }
    }
    
}
