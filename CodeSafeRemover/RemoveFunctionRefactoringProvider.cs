using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace RemoveFunctionRefactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RemoveFunctionRefactoringProvider)), Shared]
    public class RemoveFunctionRefactoringProvider : CodeRefactoringProvider
    {
        private const string TargetFunctionName = "OldFunction"; // تابعی که باید حذف بشه

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var node = root.FindNode(context.Span);

            // اگر مکان‌نما روی نام تابع بود
            if (node is MethodDeclarationSyntax methodDecl &&
                methodDecl.Identifier.Text == TargetFunctionName)
            {
                var action = CodeAction.Create(
                    title: $"Remove function '{TargetFunctionName}'",
                    createChangedDocument: c => RemoveFunctionAsync(context.Document, methodDecl, c),
                    equivalenceKey: "RemoveFunction");

                context.RegisterRefactoring(action);
            }
        }

        private async Task<Document> RemoveFunctionAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.RemoveNode(methodDecl, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
