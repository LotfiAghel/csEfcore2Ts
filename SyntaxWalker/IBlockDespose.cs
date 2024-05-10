using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace SyntaxWalker
{
    public interface IBlockDespose
    {
        BlockDespose newBlock(string text);
        ClassBlock newClass(string text);
        BlockDespose newFunction(string name,List<Tuple<string,string>> args,string returnType,bool isAsync=false);
        void WriteLine(string text);
        string getFileName();
        void addUsedType(ITypeSymbol type);
    }
}
