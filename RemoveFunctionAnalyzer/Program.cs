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
            Console.WriteLine("Usage:");
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
                newRoot=RemoveMethodAndReplaceInvocations(root, methodNode, functionName, replacementText);
                
            }
            else
            {
                newRoot=RemoveClassAndReplaceUsages(root, classNode, replacementText);
            }

            var newSource = newRoot.ToFullString();

            // Overwrite the file with the new source code
            await File.WriteAllTextAsync(filePath, newSource);

            Console.WriteLine($"Function '{functionName}' removed and invocations replaced with '{replacementText}' successfully.");

            return 0;
        }

        public static bool checkEqul(VariableDeclaratorSyntax vard, ClassDeclarationSyntax classdec, string[] baseClassNames)
        {
            if (vard.Parent is VariableDeclarationSyntax decl && decl.Type is IdentifierNameSyntax id)
            {
                string typeName = id.Identifier.Text;
                // Remove variables declared with the removed class type or any of its base classes
                return typeName == classdec.Identifier.Text || baseClassNames.Contains(typeName);
            }
            return false;
        }
        public static SyntaxNode RemoveClassAndReplaceUsages(SyntaxNode root, ClassDeclarationSyntax classNode, string replacementText)
        {
            SyntaxNode newRoot = root.RemoveNode(classNode, SyntaxRemoveOptions.KeepNoTrivia);

            string removedClassName = classNode.Identifier.Text;

            // Extract base class names of the removed class
            string[] baseClassNames = Array.Empty<string>();
            if (classNode.BaseList != null)
            {
                baseClassNames = classNode.BaseList.Types
                    .Select(t =>
                    {
                        if (t.Type is IdentifierNameSyntax id)
                            return id.Identifier.Text;
                        else if (t.Type is QualifiedNameSyntax qn)
                            return qn.Right.Identifier.Text;
                        else
                            return null;
                    })
                    .Where(n => n != null)
                    .ToArray();
            }

            if (replacementText == "0")
            {
                // Remove variable declarations of the removed class or its base classes
                var variableDeclarators = newRoot.DescendantNodes()
                    .OfType<VariableDeclaratorSyntax>()
                    .Where(v => checkEqul(v, classNode, baseClassNames))
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

                // Remove fields of any class with type classNode or its base classes
                var fieldDeclarations = newRoot.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field =>
                    {
                        var type = field.Declaration.Type;
                        string typeName = null;
                        if (type is IdentifierNameSyntax id)
                            typeName = id.Identifier.Text;
                        else if (type is QualifiedNameSyntax qn)
                            typeName = qn.Right.Identifier.Text;
                        return typeName == removedClassName || baseClassNames.Contains(typeName);
                    })
                    .ToList();
                

                
                foreach (var field in fieldDeclarations)
                {
                    newRoot = newRoot.RemoveNode(field, SyntaxRemoveOptions.KeepNoTrivia);
                }

                {//TODO Remove fields of any class with type classNode or its base classes in this block
                    
                }

                // Helper function to get base type name as string
                string GetBaseTypeName(TypeSyntax type)
                {
                    if (type is IdentifierNameSyntax id)
                        return id.Identifier.Text;
                    else if (type is QualifiedNameSyntax qn)
                        return qn.Right.Identifier.Text;
                    else
                        return null;
                }

                // Remove classes that inherit from the removed class
                var derivedClasses = newRoot.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.BaseList != null &&
                                c.BaseList.Types.Any(t => GetBaseTypeName(t.Type) == removedClassName))
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
                    .Where(id => id.Identifier.Text == classNode.Identifier.Text)
                    .ToList();

                // Replace each identifier with the replacement text as a literal expression
                foreach (var identifier in identifiers)
                {
                    var replacementNode = SyntaxFactory.ParseExpression(replacementText)
                        .WithTriviaFrom(identifier);

                    newRoot = newRoot.ReplaceNode(identifier, replacementNode);
                }
            }
            return newRoot;
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