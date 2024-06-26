using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.ts
{
    public class FileBlock : BlockDespose, IFileBlock
    {
        //public IBaseWriter writer;
        public string fn { get; set; }



        public FileBlock(string name, BlockDespose parnet, int tab) : base(name, parnet, tab)
        {
        }


       
        public virtual IBlockDespose newNameSpace(string name)
        {
            return new BlockDespose($"export namespace {name}   ", this, tab + 1);
        }

        public override string getFileName()
        {
            return fn;
        }

        



    }

}
