using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static SyntaxWalker.Program;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
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
    public class BlockDespose :  IBlockDespose
    {
        public string header;
        public List<IBlockDespose> lines = new() { };
        public int tab = 0;
        IBlockDespose parnet;
        public bool braket=false;
        public HashSet<ITypeSymbol> usedTypes = new();
        public BlockDespose(string name, IBlockDespose parnet,int tab)
        {
            this.header = name;
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

            var b= new BlockDespose(text, this, tab + 1);
            lines.Add(b);
            return b;
        }
        public void WriteLine(string text)
        {
            var b = new BlockDespose(text, this,tab+1);
            lines.Add(b);
            
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
            return newBlock($"{(isAsync ? "async" : "")} {name}({argsS}){(returnType != null ? $":{returnType}" : "")}");

        }

        public IClassBlock newClass(string text)
        {
           
                var z = ILangSuport.Instance.newClassBlock(text, this, tab + 1);
                lines.Add(z);
                return z;
            
        }
        public override string ToString()
        {
            var txt = "";
            for (var i = 0; i < tab; ++i)
                txt += "\t";
            txt+=header;
            if(lines.Count > 0 || braket) {
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
            txt+= "\n";
            return txt;
        }


    }

}
