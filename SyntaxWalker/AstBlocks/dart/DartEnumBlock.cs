using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.Dart
{
    
   
    public class DartEnumBlock : DartBlockDespose, IEnumBlock
    {
        EnumDeclarationSyntax class_;
        SemanticModel sm;
        Dictionary<string,int> fields = new Dictionary<string,int>();
        public void addField(EnumMemberDeclarationSyntax mem, int rmp2)
        {
            fields[mem.Identifier.ToString()] = rmp2;
        }
     
        public DartEnumBlock(EnumDeclarationSyntax class_, SemanticModel sm, IBlockDespose parnet, int tab) : base($"enum {class_.getName()}", parnet, tab)
        {
            this.class_ = class_;
            this.sm = sm;
            braket = true;
            List<string> fields=new List<string>();
            foreach (var mem in class_.Members)
                {
                    //tt.WriteLine($"{mem.Identifier}");


                    var v = mem.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                    if (v != null)
                    {
                        var rmp2 = sm.GetConstantValue(v.Value);


                        fields.Add($"{mem.Identifier}(value:{rmp2})");
                    

                    }
                    
                    
                    

                }
            WriteLine($"{fields.agregate2(",\n")};");
            WriteLine("final int value;");
            newConstructor(null,sm);
            WriteLine($"static {class_.getName()} fromJson(int x) {{\n return {class_.getName()}.values.where((y) => y.value == x).first;\n}}");
            WriteLine($"int toJson() {{\n return value;\n}}");
        }
        public BlockDespose newConstructor(List<IPropertySymbol> flatProps, SemanticModel sm)
        {
            return newBlock($"const {class_.getName()}({{required this.value}});");


        }
        public void addField(PropertyDeclarationSyntax f, ITypeSymbol type, SemanticModel sm)
        {
            
        }
     
        
        public void toJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            using (var hed = this.newFunction("toJson", new List<IPropertySymbol>() { }, "Map<String, dynamic>")) //TODO isclientCreatble or not 
            {
                hed.WriteLine($"return value;");
            }
        }
        public void fromJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            this.WriteLine($"{name}.fromJson(Map<String, dynamic> json):"); //TODO isclientCreatble or not 
            {


              
                
                var res=new List<string>();

                
                this.WriteLine($";");

            }
        }
        public override string ToString()
        {
             return base.ToString();
        }


    }

}
