using Microsoft.CodeAnalysis;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.Dart
{
    public class DartFileBlock : DartBlockDespose,IFileBlock
    {
        //public IBaseWriter writer;
        public string fn { get;set; }
        
      

        public DartFileBlock(string name, BlockDespose parnet, int tab) : base(name, parnet, tab )
        {
        }

      
        
        public virtual DartBlockDespose newNameSpace(string name)
        {
            return new DartBlockDespose($"export namespace {name}   ", this,tab+1);
        }

        public override string getFileName()
        {
            return fn;
        }

    
       

       
    }
  
}
