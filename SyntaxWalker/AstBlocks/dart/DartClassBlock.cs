using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.Dart
{
    public class Dart : ILangSuport
    {
        public static Dictionary<string, TsTypeInf> tsMap = new() { { "int", new TsTypeInf("int") },{ "float", new TsTypeInf("Float")} ,
         { "Int32",  new TsTypeInf("int") },
         { "string",  new TsTypeInf("String") },
         { "Boolean",  new TsTypeInf("bool") },
         
         { "Int64",  new TsTypeInf("int") },
         { "Decimal",  new TsTypeInf("float") },
        { "long",  new TsTypeInf("int") },
        { "Single",  new TsTypeInf("int") },
            { "DateTimeOffset", new TsTypeInf("DateTime")},
            { "DateTime", new TsTypeInf("DateTime")}
        };
       
        public string getPath(string fn)
        {
            return $"D:\\programing\\TestHelperTotal\\testhelper-flutter\\lib\\generated_models\\{fn}.dart";
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

        public IClassBlock newClassBlock(TypeDeclarationSyntax class_, SemanticModel sm, IBlockDespose fileBlock, int v)
        {
            return new DartClassBlock(class_, sm, fileBlock,v);
        }

        public IFileBlock newFileBlock(string fn2)
        {
            return new DartFileBlock(null, null, 0) { fn = fn2 };
        }

        public void ImportBasic(FileWriter fwriter)
        {
            fwriter.WriteLine("import 'package:TestHelper/basics/basics.dart';");
        }
        public void ImportWrite(ITypeSymbol tf, FileWriter fwriter, Dictionary<ITypeSymbol, TypeDes> managerMap)
        {
            var cs = $"  ";
            fwriter.WriteLine($"import 'package:TestHelper/Models/{managerMap[tf].fn.linuxPathStyle()}.dart';");



        }

        
    }
    public class DartClassBlock : DartBlockDespose, IClassBlock
    {
        TypeDeclarationSyntax class_;
        SemanticModel sm;


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
        public DartClassBlock(TypeDeclarationSyntax class_, SemanticModel sm, IBlockDespose parnet, int tab) : base($"class {class_.getName()}  {getHeaderClass(class_,  sm)} ", parnet, tab)
        {
            this.class_ = class_;
            this.sm = sm;
            braket = true;
        }
        public BlockDespose newConstructor(ITypeSymbol superClassSymbol, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps, SemanticModel sm)
        {
            return newBlock("");


            var argsS = superClassProps.ToList().ConvertAll(x => $"{(x.Type.isNullable() ? "" : "required")} super.{x.Name}");
            argsS.AddRange(flatProps.ToList().ConvertAll(x => $"{(x.Type.isNullable() ? "" : "required")} this.{x.Name}"));
            var b = newBlock($"{class_.getName()}({{ {argsS} }}):");
            b.braket = true;
            //lines.Add(b);
            


            {
                if (superClassSymbol != null)
                {
                    //superClassSymbol.getProps()
                    b.SuperCunstrocotrCall(new() { "args" });
                }
                foreach (var m in flatProps)
                    b.WriteLine($"{m.Name.toCamel()}({m.Name.toCamel()}),");
            }
            b.WriteLine($";");
            return b;
            //return newFunction( "constructor" , args,null,false);
        }
        public void addField(PropertyDeclarationSyntax f, ITypeSymbol type, SemanticModel sm)
        {
            WriteLine($"{ILangSuport.getTsName(type,sm).name} {(type.isNullable() ? "?" : "")}  {f.Identifier.ToString().toCamel()};");
        }
        public static Dictionary<string, string> map=new Dictionary<string, string>() { 
            { nameof(DateTime), "DateTime.parse" },
            { nameof(Int32), "" },
            { nameof(String), "" },
        };
        public string handleFromJson(ITypeSymbol type)
        {
            if (!map.ContainsKey(type.Name))
                return $"{ILangSuport.getTsName(type, sm).name}.fromJson";
            return map[type.Name];
        }
        public static Dictionary<string, string> map2 = new Dictionary<string, string>() {
            { nameof(DateTime), "DateTime.toStr" },
            { nameof(DateOnly), "DateTime.toStr" },
            { "Date", "Date.toStr" },
            { nameof(Int32), "" },
            { nameof(Boolean), "" },
            { nameof(String), "" },
        };
        public string handleToJson(ITypeSymbol type)
        {
            if (!map2.ContainsKey(type.Name))
                return $"{ILangSuport.getTsName(type, sm).name}.toJson";
            return map2[type.Name];
        }
        public void toJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            var baseClass = class_.getBaseClass(sm);

            using (var hed = this.newFunction("toJson", new List<IPropertySymbol>() { }, "Map<String, dynamic>")) //TODO isclientCreatble or not 
            {


                /*
                 * 
@override
  Map<String, dynamic> toJson() {
    final data = super.toJson();
    data["\$type"] = type;
    return data;
  }
                */
                if(baseClass!=null)
                    hed.WriteLine($"final data = super.toJson();");
                else
                    hed.WriteLine($"final data = <String, dynamic>{{}};");
                hed.WriteLine($" data[\"\\$type\"]=\"{fullname}\";");
                foreach(var pr in flatProps)
                {
                    //map["EnterDate"] = enterDate.toString();
                    if (pr.Type.isNullable())
                        hed.WriteLine($"data['{pr.Name.toCamel()}'] = {pr.Name} != null ? {handleToJson(pr.Type)}('{pr.Name.toCamel()}') : null;");
                    else
                        hed.WriteLine($"data['{pr.Name.toCamel()}'] = {handleToJson(pr.Type)}({pr.Name.toCamel()});");
                }
                hed.WriteLine($"return data;");

            }
        }
        public void fromJson(string name, string fullname, List<IPropertySymbol> superClassProps, List<IPropertySymbol> flatProps)
        {
            this.WriteLine($"{name}.fromJson(Map<String, dynamic> json):"); //TODO isclientCreatble or not 
            {


                /*
                 * 
ResponseModel.fromJson(Map<String, dynamic> json):
      type = json['\$type'],
        enterDate = json['EnterDate'] != null ?  DateTime.parse(json['EnterDate'] ):DateTime.now(),
        questionId = json['QuestionId'],
        serverId = json['serverId'],
        examPartSessionId = json['examPartSessionId'],
        super.fromJson(json);
                */
                var baseClass = class_.getBaseClass(sm);
                if (baseClass!=null)
                    this.WriteLine($"super.fromJson(json),");
                foreach (var pr in flatProps)
                {
                    //enterDate = json['EnterDate'] != null ? DateTime.parse(json['EnterDate']) : DateTime.now(),
                    if (pr.Type.isNullable())
                    {
                        var s = (pr.Type as INamedTypeSymbol).TypeArguments.FirstOrDefault();
                        this.WriteLine($"{pr.Name.toCamel()} = json['{pr.Name.toCamel()}'] != null ? {handleFromJson(s)}(json['{pr.Name.toCamel()}']) : null,");
                    }
                    else
                        this.WriteLine($"{pr.Name.toCamel()} = {handleFromJson(pr.Type)}(json['{pr.Name.toCamel()}']),");
                }
                this.WriteLine($";");

            }
        }


        public void CreatorPolimorphic(ITypeSymbol clk, TypeDes clv, Dictionary<ITypeSymbol, TypeDes> managerMap)
        {
            if (clv.syntax != null && clv.isNonAbstractClass && !clv.isPolimorphicBase)
            {
                WriteLine($"static {clv.syntax.getName()} createFromJson(Map<String, dynamic> json) {{\r\n    String type = json[\"\\$type\"];\r\n    return creators[type]!(json);\r\n  }}");
                return;
            }

            if (clv.syntax != null && clv.isNonAbstractClass)
            {
                // static Map<String, QuestionModel Function(dynamic json)> creators = {
                WriteLine($"static Map<String, {clv.syntax.getName()} Function(dynamic json)> creators ={{");
                WriteLine($"\"{clk}\":(dynamic json)=> {clv.syntax.getName()}.fromJson(args),");

                foreach (var e in managerMap)
                {
                    if (e.Value.syntax == null)
                        continue;
                    var bases = e.Value.syntax.GetBaseClasses(e.Value.sm);


                    if (bases.Contains(clk))
                    {
                        WriteLine($"\"{e.Key}\":(dynamic json)=> {e.Key.Name}.fromJson(args),");
                    }
                }
                WriteLine("};");
                WriteLine($"if(!(\"$type\" in args)){{\n\treturn  {clv.syntax.getName()}.fromJson(args);}}");
                WriteLine("return types[args[\"$type\"]](args);");
                WriteLine("}");
            }
        }
    }

}
