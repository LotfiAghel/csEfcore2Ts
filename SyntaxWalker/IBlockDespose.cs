using Microsoft.CodeAnalysis;

namespace SyntaxWalker
{
    public interface IBlockDespose
    {
        BlockDespose newBlock(string text);
        void WriteLine(string text);
        string getFileName();
        void addUsedType(ITypeSymbol type);
    }
}
