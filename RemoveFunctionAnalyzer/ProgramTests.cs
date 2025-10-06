using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using System.Linq;

namespace RemoveFunctionAnalyzer.Tests
{
    public class ProgramTests
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
        public async Task RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls2()
        {
            var tree = CSharpSyntaxTree.ParseText(BeforeCode);
            var root = await tree.GetRootAsync();

            var methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Foo");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveMethodAndReplaceInvocations(root, methodNode, "Foo", "0");

            var newCode = newRoot.ToFullString();

            Assert.Equal(AfterCode.Replace("\r\n", "\n"), newCode.Replace("\r\n", "\n"));
        }

        [Fact]
        public async Task RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls()
        {
            var code = @"
class C {
    void Foo() {}
    void Bar() {
        Foo();
        Foo();
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Foo");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveMethodAndReplaceInvocations(root, methodNode, "Foo", "0");

            var newCode = newRoot.ToFullString();

            Assert.DoesNotContain("void Foo()", newCode);
            Assert.DoesNotContain("Foo()", newCode);
            Assert.Contains("0;", newCode);
        }

        [Fact]
        public async Task RemoveClassAndReplaceUsages_ReplacesUsages()
        {
            var code = @"
class C {}
class D {
    C c;
    void M() {
        C local = null;
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveClassAndReplaceUsages(root, classNode, "C", "null");

            var newCode = newRoot.ToFullString();

            Assert.DoesNotContain("class C", newCode);
            Assert.Contains("null c;", newCode);
            Assert.Contains("null local = null;", newCode);
        }

        [Fact]
        public async Task RemoveClassAndReplaceUsages_WithZero_RemovesDeclarationsAndDerivedClasses()
        {
            var code = @"
class Base {}
class Derived : Base {}
class C {}
class D {
    C c;
    Base b;
    void M() {
        C local = null;
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveClassAndReplaceUsages(root, classNode, "C", "0");

            var newCode = newRoot.ToFullString();

            Assert.DoesNotContain("class C", newCode);
            Assert.DoesNotContain("C c;", newCode);
            Assert.DoesNotContain("C local = null;", newCode);
            Assert.Contains("class Derived : Base", newCode); // Derived class unrelated to C remains
        }
    }
}