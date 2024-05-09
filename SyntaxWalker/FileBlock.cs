using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Xml.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
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
            return new BlockDespose($"namespace {name}   ", this);
        }

        public string getFileName()
        {
            return fn;
        }

        public void addUsedType(ITypeSymbol type)
        {
            usedTypes.Add(type);    
        }
    }
    
}
