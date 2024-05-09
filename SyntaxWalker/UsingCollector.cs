using static System.Console;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SyntaxWalker
{
    // <Snippet3>
    class UsingCollector : CSharpSyntaxWalker
    // </Snippet3>
    {
        // <Snippet4>
        public ICollection<NameSyntax> Usings { get; } = new List<NameSyntax>();
        public Compilation compilation { get; internal set; }

        // </Snippet4>
        public SemanticModel model;
        public int t = 0;

        public override void Visit(SyntaxNode node)
        {
            Console.WriteLine($"{t} {node.Kind()} {node.ToString()}");
            ++t;
            if (node.ToString() == "LocalizedStrings")
                Console.WriteLine("");
            bool ph = false;
            if (node is ClassDeclarationSyntax ns)
            {
                ph= true;
                if(ns.Identifier.ToString()== "LocalizedStrings")
                {



                    model = compilation.GetSemanticModel(node.SyntaxTree);
                    Console.WriteLine(ns.Identifier.ToString());
                }
            }
            if (model!=null && node is IdentifierNameSyntax ns2)
            {
                
                
                //if(ns2.ToString()== "ReplayServer")
                {
                    var nn= model.GetSymbolInfo(ns2);
                    
                    Console.WriteLine(nn.ToString());
                    var sym = nn.Symbol;
                    if (sym is null)// catch nameof :-?
                    {
                        Console.WriteLine(ns2.ToString());
                        Usings.Add(ns2 as NameSyntax);
                        
                    }
                    //sym.
                }
                
            }
            
            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var b = node.ChildNodes().ToList();
                if (b[0].ToString() == "LocalizedStrings")
                {
                    Usings.Add(b[1] as NameSyntax);
                    //Console.WriteLine(b[1].ToString());
                }
            }
            base.Visit(node);
            if (ph)
                model = null;
            --t;
        }

        
    }





    class GenerateTs : CSharpSyntaxWalker
    // </Snippet3>
    {
        // <Snippet4>
        public ICollection<NameSyntax> Usings { get; } = new List<NameSyntax>();
        public Compilation compilation { get; internal set; }

        // </Snippet4>
        public SemanticModel model;
        public int t = 0;
        public string filePath;

        public override void Visit(SyntaxNode node)
        {
            Console.WriteLine($"{t} {node.Kind()} {node.ToString()}");
            ++t;
            bool ph = false;
            
            if (node is ClassDeclarationSyntax ns)
            {
                ph = true;
                if (ns.Identifier.ToString() == "LocalizedStrings")
                {



                    model = compilation.GetSemanticModel(node.SyntaxTree);
                    Console.WriteLine(ns.Identifier.ToString());
                }
            }
            if (model != null && node is IdentifierNameSyntax ns2)
            {


                //if(ns2.ToString()== "ReplayServer")
                {
                    var nn = model.GetSymbolInfo(ns2);

                    Console.WriteLine(nn.ToString());
                    var sym = nn.Symbol;
                    if (sym is null)// catch nameof :-?
                    {
                        Console.WriteLine(ns2.ToString());
                        Usings.Add(ns2 as NameSyntax);

                    }
                    //sym.
                }

            }

            if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                var b = node.ChildNodes().ToList();
                if (b[0].ToString() == "LocalizedStrings")
                {
                    Usings.Add(b[1] as NameSyntax);
                    //Console.WriteLine(b[1].ToString());
                }
            }
            base.Visit(node);
            if (ph)
                model = null;
            --t;
        }


    }


}
