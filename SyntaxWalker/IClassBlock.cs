using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SyntaxWalker
{
    public class PropInf
    {
        public bool fromSuper;
        

        public PropInf(string v, TsTypeInf tsTypeInf, bool v1=false)
        {
            this.name = v;
            this.type = tsTypeInf;
            this.fromSuper = v1;
        }

        public string name { get; set; }
        public TsTypeInf type { get; set; }
    }
    public interface IClassBlock: IBlockDespose
    {
        BlockDespose newConstructor(List<PropInf> args);
        void toJson(string name, string fullname, List<PropInf> args);
        void fromJson(string name, string fullname, List<PropInf> args);
    }
}