using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.ts
{
    public class TS : ILangSuport
    {
        public static Dictionary<string, TsTypeInf> tsMap = new() { { "int", new TsTypeInf("number") },{ "float", new TsTypeInf("number")} ,
         { "Int32",  new TsTypeInf("number") },
         { "String",  new TsTypeInf("string") },
         { "Int64",  new TsTypeInf("number") },
         { "Decimal",  new TsTypeInf("number") },
        { "long",  new TsTypeInf("number") },
        { "Single",  new TsTypeInf("number") },
            { "DateTimeOffset", new TsTypeInf("Date")},
            { "DateTime", new TsTypeInf("Date")}
        };



        public IClassBlock newClassBlock(TypeDeclarationSyntax class_, SemanticModel sm, IBlockDespose fileBlock, int v)
        {
            return new ClassBlock(class_,sm,fileBlock,v);
        }
        public IFileBlock newFileBlock(string fn2)
        {
            return new FileBlock(null, null, 0) { fn = fn2 };
        }

        public TsTypeInf getTsName(string type)
        {
            var x = new TsTypeInf(type);
            if (x.name.EndsWith('?'))
            {
                x.name = x.name.Substring(0, x.name.Length - 1);
                var z = getTsName(x.name);
                z.nullable = true;
                return z;
            }
            TsTypeInf res;
            if (tsMap.TryGetValue(type, out res))
                return res;

            return x;
        }
        public string getPath(string fn)
        {
            return  $"D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\{fn}.ts";
            
        }

        public void ImportWrite(ITypeSymbol tf, FileWriter fwriter, Dictionary<ITypeSymbol, TypeDes> managerMap)
        {
            var cs = $"  ";
            fwriter.WriteLine($"import {{ {tf.Name}  }} from \"Models/{managerMap[tf].fn.linuxPathStyle()}\"");


        }
        public void ImportBasic(FileWriter fwriter)
        {
            
            fwriter.WriteLine("import  { Guid,Forg,httpGettr,List,Dictionary,ForeignKey,ForeignKey2,Rial,NpgsqlRange } from \"Models/base\";");

        }
    }

    public class ClassBlock : BlockDespose, IClassBlock
    {


        
        public static string getHeaderClass(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            string res = "";


            var baseClass = class_.getBaseClass(sm);

            var interfaces = class_.getInterfaces(sm);
            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            
            if (baseClass != null)
                res += $" extends {ILangSuport.getTsName(baseClass, sm).name}";
            if (interfaces.Count() > 0)
                res += $" implements {interfaces.ConvertAll(x => ILangSuport.getTsName(x, sm).name).Aggregate((l, r) => $"{l},{r}")}";
            return res;
        }
        public ClassBlock(TypeDeclarationSyntax class_, SemanticModel sm, IBlockDespose parnet, int tab) : base($"export class {class_.getName()}  {getHeaderClass(class_,  sm)} ", parnet, tab)
        {
            braket = true;
        }

        public void fromJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            return;
        }
        public BlockDespose newConstructor(ITypeSymbol superClassSymbol, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps, SemanticModel sm)
        {
            //getTsName(sm.GetTypeInfo(x.Type).Type, sm);
            
            var argsS = superClassProps.ToList().ConvertAll(x => $"{x.Name.toCamel()}{(x.Type.isNullable() ? "?" : "")}:{ILangSuport.getTsName(x.Type,sm).name}");
            argsS.AddRange( flatProps.ToList().ConvertAll(x => $"{x.Name.toCamel()}{(x.Type.isNullable() ? "?" : "")}:{ILangSuport.getTsName(x.Type, sm).name}"));
            var b = newBlock($"constructor(args:{{ {argsS.agregate()}  }})");
            b.braket = true;
            //lines.Add(b);
            {
                if (superClassSymbol != null)
                {
                    //superClassSymbol.getProps()
                    b.SuperCunstrocotrCall(new() { "args" });
                }
                foreach (var m in flatProps)
                    b.WriteLine($"this.{m.Name.toCamel()} = args.{m.Name.toCamel()};");
            }
            return b;
        }
       
        public void toJson(string name,string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            using (var hed = this.newFunction("toJson", new List<IPropertySymbol>() { }, name)) //TODO isclientCreatble or not 
            {


                /*
                 * //@ts-ignore
                this["$type"]="Models.ExamAction";
                return this;
                */
                
                hed.WriteLine($" //@ts-ignore");
                hed.WriteLine($" this[\"$type\"]=\"{fullname}\"");
                hed.WriteLine($" return this;");

            }
        }


        public void CreatorPolimorphic(ITypeSymbol clk, TypeDes clv, Dictionary<ITypeSymbol, TypeDes> managerMap)
        {
            if (clv.syntax != null && clv.isNonAbstractClass && !clv.isPolimorphicBase)
            {
                WriteLine($"static Creator(args:any):{clv.syntax.getName()}{{return new {clv.syntax.getName()}(args);}}");
                //WriteLine($"export var {clv.syntax.getName()}Creator = (args:any)=> new {clv.syntax.getName()}(args)");
                return;
            }
            
            if (clv.syntax != null && clv.isNonAbstractClass)
            {
                WriteLine($"static Creator(args:any):{clv.syntax.getName()}{{const types={{");
                WriteLine($"\"{clk}\":(args: any)=>new {clv.syntax.getName()}(args),");

                foreach (var e in managerMap)
                {
                    if (e.Value.syntax == null)
                        continue;
                    var bases = e.Value.syntax.GetBaseClasses(e.Value.sm);


                    if (bases.Contains(clk))
                    {
                        WriteLine($"\"{e.Key}\":(args: any)=>new {e.Key.Name}(args),");
                    }
                }
                WriteLine("};");
                WriteLine($"if(!(\"$type\" in args)){{\treturn new {clv.syntax.getName()}(args);}}");
                WriteLine("return types[args[\"$type\"]](args);");
                WriteLine("}");
            }
        }

        public void addField(PropertyDeclarationSyntax f, ITypeSymbol type, SemanticModel sm)
        {
            WriteLine($"{f.Identifier.ToString().toCamel()}{(type.isNullable() ? "?" : "")} : {ILangSuport.getTsName(type, sm).name};");
        }
    }

}
