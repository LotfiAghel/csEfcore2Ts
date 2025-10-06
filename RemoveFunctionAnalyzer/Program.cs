using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RemoveFunctionAnalyzer
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: RemoveFunctionAnalyzer <file-path> <function-name>");
                return 1;
            }

            var filePath = args[0];
            var functionName = args[1];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return 1;
            }

            var sourceCode = await File.ReadAllTextAsync(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = await syntaxTree.GetRootAsync();

            var methodNode = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == functionName);

            if (methodNode == null)
            {
                Console.WriteLine($"Function '{functionName}' not found in file.");
                return 1;
            }

            var newRoot = root.RemoveNode(methodNode, SyntaxRemoveOptions.KeepNoTrivia);

            var newSource = newRoot.ToFullString();

            // Overwrite the file with the new source code
            await File.WriteAllTextAsync(filePath, newSource);

            Console.WriteLine($"Function '{functionName}' removed successfully.");

            return 0;
        }
    }
}