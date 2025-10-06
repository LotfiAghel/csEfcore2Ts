using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using System.Linq;

namespace RemoveFunctionAnalyzer.Tests
{
    public class RemoveMethodTests
    {
        private const string BeforeCode = @"
class C {
    void Foo() {}
    void Bar() {
        Foo();
        Foo();
    }
}";

        private const string AfterCode = @"
class C {
    void Bar() {
        0;
        0;
    }
}";

        [Fact]
        public async Task RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls()
        {
            var tree = CSharpSyntaxTree.ParseText(BeforeCode);
            var root = await tree.GetRootAsync();

            var methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Foo");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveMethodAndReplaceInvocations(root, methodNode, "Foo", "0");

            var newCode = newRoot.ToFullString();

            Assert.Equal(AfterCode.Replace("\r\n", "\n"), newCode.Replace("\r\n", "\n"));
        }
    }
}