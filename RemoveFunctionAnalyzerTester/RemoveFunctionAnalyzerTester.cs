using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RemoveFunctionAnalyzer;

namespace RemoveFunctionAnalyzerTester
{
    [TestClass]
    public class RemoveFunctionAnalyzerTester
    {
       
              

        [TestMethod]
        public async Task RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls2()
        {
            Console.WriteLine("run RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls2 ");
            var beforePath = @"../../../../tests/RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls/before/C.cs";
            var afterPath = @"../../../../tests/RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls/after/C.cs";

            var beforeCode = System.IO.File.ReadAllText(beforePath);
            var afterCode = System.IO.File.ReadAllText(afterPath);

            var tree = CSharpSyntaxTree.ParseText(beforeCode);
            var root = await tree.GetRootAsync();
            
            var methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Foo");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveMethodAndReplaceInvocations(root, methodNode, "Foo", "0");

            var newCode = newRoot.ToFullString();
            
            
            Assert.AreEqual(afterCode.Replace("\r\n", "\n"), newCode.Replace("\r\n", "\n"));
        }

        [TestMethod]
        public async Task RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls()
        {
            var beforePath = @"../../../../tests/RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls/before/C.cs";
            var afterPath = @"../../../../tests/RemoveMethodAndReplaceInvocations_RemovesMethodAndReplacesCalls/after/C.cs";

            var beforeCode = System.IO.File.ReadAllText(beforePath);
            var afterCode = System.IO.File.ReadAllText(afterPath);

            var tree = CSharpSyntaxTree.ParseText(beforeCode);
            var root = await tree.GetRootAsync();

            var methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Foo");

            var newRoot = RemoveFunctionAnalyzer.Program.RemoveMethodAndReplaceInvocations(root, methodNode, "Foo", "0");

            var newCode = newRoot.ToFullString();

            
            Assert.AreEqual(afterCode.Replace("\r\n", "\n"), newCode.Replace("\r\n", "\n"));
        }

        
         [TestMethod]
         public async Task RemoveClassAndReplaceUsages_ReplacesUsages()
         {
             var beforePath = @"../../../../tests/RemoveClassAndReplaceUsages_ReplacesUsages/before/C.cs";
             var afterPath = @"../../../../tests/RemoveClassAndReplaceUsages_ReplacesUsages/after/C.cs";

             var beforeCode = System.IO.File.ReadAllText(beforePath);
             var afterCode = System.IO.File.ReadAllText(afterPath);

             var tree = CSharpSyntaxTree.ParseText(beforeCode);
             var root = await tree.GetRootAsync();

             var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "C");

             var newRoot = RemoveFunctionAnalyzer.Program.RemoveClassAndReplaceUsages(root, classNode,  "0");

             var newCode = newRoot.ToFullString();
             Console.WriteLine(newCode);
             Console.WriteLine(afterCode);
             Assert.AreEqual(afterCode.Replace("\r\n", "\n"), newCode.Replace("\r\n", "\n"));
         }
        

        
         [TestMethod]
         public async Task RemoveClassAndReplaceUsages_WithZero_RemovesDeclarationsAndDerivedClasses()
         {
             var beforePath = @"../../../../tests/RemoveClassAndReplaceUsages_WithZero_RemovesDeclarationsAndDerivedClasses/before/C.cs";
             var afterPath = @"../../../../tests/RemoveClassAndReplaceUsages_WithZero_RemovesDeclarationsAndDerivedClasses/after/C.cs";

             var beforeCode = System.IO.File.ReadAllText(beforePath);
             var afterCode = System.IO.File.ReadAllText(afterPath);

             var tree = CSharpSyntaxTree.ParseText(beforeCode);
             var root = await tree.GetRootAsync();

             var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Base");

             var newRoot = RemoveFunctionAnalyzer.Program.RemoveClassAndReplaceUsages(root, classNode,  "0");

             var newCode = newRoot.ToFullString();
             Console.WriteLine(newCode);
             Console.WriteLine(afterCode);
             Assert.AreEqual(afterCode.Replace("\r\n", "\n").Replace("\t"," ").Replace("\n"," ").Replace(" ",""), newCode.Replace("\r\n", "\n").Replace("\t"," ").Replace("\n"," ").Replace(" ",""));
         }
        
    }
}