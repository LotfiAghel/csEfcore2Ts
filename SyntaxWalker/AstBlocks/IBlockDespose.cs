using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SyntaxWalker
{
    public interface ILangSuport
    {
        public static ILangSuport Instance;

        IClassBlock newClassBlock(string text, IBlockDespose fileBlock, int v);
    }
    public interface IBlockDespose: IDisposable
    {
        BlockDespose newBlock(string text);
        IClassBlock newClass(string text);
        BlockDespose newFunction(string name,List<Tuple<string,string>> args,string returnType,bool isAsync=false);
        void WriteLine(string text);
        string getFileName();
        void addUsedType(ITypeSymbol type);
    }
}
