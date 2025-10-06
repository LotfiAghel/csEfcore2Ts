using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace RemoveFunctionAnalyzer
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RemoveFunctionRefactoringProvider)), Shared]
    public class RemoveFunctionRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var node = root.FindNode(context.Span);
            if (node is MethodDeclarationSyntax methodDecl)
            {
                var action = CodeAction.Create(
                    $"Remove function '{methodDecl.Identifier.Text}'",
                    c => RemoveFunctionAsync(context.Document, methodDecl, c),
                    nameof(RemoveFunctionRefactoringProvider));

                context.RegisterRefactoring(action);
            }
        }

        private async Task<Document> RemoveFunctionAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var newRoot = root.RemoveNode(methodDecl, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
