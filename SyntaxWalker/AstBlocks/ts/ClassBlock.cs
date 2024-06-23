using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.ts
{
    public class TS : ILangSuport
    {
        public IClassBlock newClassBlock(string text, IBlockDespose fileBlock, int v)
        {
            return new ClassBlock(text,fileBlock,v);
        }
    }
    public class ClassBlock : BlockDespose, IClassBlock
    {


        public HashSet<ITypeSymbol> usedTypes = new();

        public ClassBlock(string name, IBlockDespose parnet, int tab) : base(name, parnet, tab)
        {
            braket = true;
        }

        public void fromJson(string name, string fullname, List<PropInf> args)
        {
            return;
        }

        public BlockDespose newConstructor(List<PropInf> args)
        {
            string clsName = this.header;


            var argsS = args.ToList().ConvertAll(x => $"{(x.type.nullable ? "" : "required")} {(x.fromSuper?"super":"this")}.{x.name}").agregate();
            var b = newBlock($"{clsName}({{ {argsS} }});");
            b.braket = true;
            //lines.Add(b);
            return b;
            //return newFunction( "constructor" , args,null,false);
        }
        public void toJson(string name,string fullname, List<PropInf> args)
        {
            using (var hed = this.newFunction("toJson", new List<Tuple<string, string>>() { }, name)) //TODO isclientCreatble or not 
            {


                /*
                 * //@ts-ignore
                this["$type"]="Models.ExamAction";
                return this;
                */
                
                hed.WriteLine($" //@ts-ignore");
                hed.WriteLine($" this[\"$type\"]=\"{fullname}\"");
                hed.WriteLine($" return this;");

            }
        }
        


    }

}
