using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public FunctionCallHead newConstructor()
        {
            return new FunctionCallHead() { Name= "constructor" };
        }

        public void functionCall(string v, List<string> list)
        {
            WriteLine($"{v}(");
            try
            {
                if(list.Count>0) 
                    WriteLine(list.Aggregate((l, r) => $"{l},{r}"));
            }
            catch
            {

            }
            WriteLine(");");
        }

        internal void SuperCunstrocotrCall(List<string> list)
        {
            functionCall("super", list);
        }
    }
    
}
