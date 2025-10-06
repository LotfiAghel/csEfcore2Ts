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
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: RemoveFunctionAnalyzer <file-path> <function-name> <replacement-text>");
                return 1;
            }

            var filePath = args[0];
            var functionName = args[1];
            var replacementText = args[2];

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

            // Find all invocation expressions of the removed function
            var invocations = newRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is IdentifierNameSyntax id && id.Identifier.Text == functionName)
                .ToList();

            // Replace each invocation with the replacement text as a literal expression
            foreach (var invocation in invocations)
            {
                var replacementNode = SyntaxFactory.ParseExpression(replacementText)
                    .WithTriviaFrom(invocation);

                newRoot = newRoot.ReplaceNode(invocation, replacementNode);
            }

            var newSource = newRoot.ToFullString();

            // Overwrite the file with the new source code
            await File.WriteAllTextAsync(filePath, newSource);

            Console.WriteLine($"Function '{functionName}' removed and invocations replaced with '{replacementText}' successfully.");

            return 0;
        }
    }
}