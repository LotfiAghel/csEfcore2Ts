using Microsoft.CodeAnalysis;

namespace SyntaxWalker.AstBlocks.ts
{
    public interface IFileBlock:IBlockDespose
    {
        string fn { get; set; }

        /*void addUsedType(ITypeSymbol type);
        string getFileName();
        IBlockDespose newBlock(string text);
        IBlockDespose newNameSpace(string name);/*/
    }
}