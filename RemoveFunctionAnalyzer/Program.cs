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
    public class Program
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

            var classNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == functionName);

            if (methodNode == null && classNode == null)
            {
                Console.WriteLine($"Function or class '{functionName}' not found in file.");
                return 1;
            }

            SyntaxNode newRoot;

            if (methodNode != null)
            {
                newRoot = root.RemoveNode(methodNode, SyntaxRemoveOptions.KeepNoTrivia);

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
            }
            else
            {
                newRoot = root.RemoveNode(classNode, SyntaxRemoveOptions.KeepNoTrivia);

                if (replacementText == "0")
                {
                    // Remove variable declarations of the removed class
                    var variableDeclarators = newRoot.DescendantNodes()
                        .OfType<VariableDeclaratorSyntax>()
                        .Where(v => v.Parent is VariableDeclarationSyntax decl &&
                                    decl.Type is IdentifierNameSyntax id &&
                                    id.Identifier.Text == functionName)
                        .ToList();

                    foreach (var variable in variableDeclarators)
                    {
                        var statement = variable.Ancestors()
                            .OfType<StatementSyntax>()
                            .FirstOrDefault();

                        if (statement != null)
                        {
                            newRoot = newRoot.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
                        }
                    }

                    // Remove classes that inherit from the removed class
                    var derivedClasses = newRoot.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Where(c => c.BaseList != null &&
                                    c.BaseList.Types.Any(t => t.Type is IdentifierNameSyntax id && id.Identifier.Text == functionName))
                        .ToList();

                    foreach (var derivedClass in derivedClasses)
                    {
                        newRoot = newRoot.RemoveNode(derivedClass, SyntaxRemoveOptions.KeepNoTrivia);
                    }
                }
                else
                {
                    // Find all identifier names of the removed class
                    var identifiers = newRoot.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.Text == functionName)
                        .ToList();

                    // Replace each identifier with the replacement text as a literal expression
                    foreach (var identifier in identifiers)
                    {
                        var replacementNode = SyntaxFactory.ParseExpression(replacementText)
                            .WithTriviaFrom(identifier);

                        newRoot = newRoot.ReplaceNode(identifier, replacementNode);
                    }
                }
            }

            var newSource = newRoot.ToFullString();

            // Overwrite the file with the new source code
            await File.WriteAllTextAsync(filePath, newSource);

            Console.WriteLine($"Function '{functionName}' removed and invocations replaced with '{replacementText}' successfully.");

            return 0;
        }

        public static SyntaxNode RemoveClassAndReplaceUsages(SyntaxNode root, MethodDeclarationSyntax methodNode, string functionName, string replacementText)
        {
            //TODO
        }
        public static SyntaxNode RemoveMethodAndReplaceInvocations(SyntaxNode root, MethodDeclarationSyntax methodNode, string functionName, string replacementText)
        {
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

            return newRoot;
        }
    }
}