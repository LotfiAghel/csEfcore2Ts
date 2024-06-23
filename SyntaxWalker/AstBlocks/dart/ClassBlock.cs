using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker.AstBlocks.Dart
{
    public class Dart : ILangSuport
    {
        public IClassBlock newClassBlock(string text, IBlockDespose fileBlock, int v)
        {
            return new ClassBlock(text,fileBlock,v);
        }
    }
    public class ClassBlock : BlockDespose, IClassBlock
    {


        public HashSet<ITypeSymbol> usedTypes = new();

        public ClassBlock(string name, IBlockDespose parnet, int tab) : base(name, parnet, tab)
        {
            braket = true;
        }
        public BlockDespose newConstructor(List<PropInf> args)
        {

            var argsS = args.ToList().ConvertAll(x => $"{x.name}{(x.type.nullable ? "?" : "")}:{x.type.name}").agregate();
            var b = newBlock($"constructor(args:{{ {argsS} }})");
            b.braket = true;
            //lines.Add(b);
            return b;
            //return newFunction( "constructor" , args,null,false);
        }
        public static Dictionary<string, string> map=new Dictionary<string, string>() { 
            { nameof(DateTime), "DateTime.parse" },
            { nameof(Int32), "" },
            { nameof(String), "" },
        };
        public string handleFromJson(TsTypeInf type)
        {
            return map[type.name];
        }
        public static Dictionary<string, string> map2 = new Dictionary<string, string>() {
            { nameof(DateTime), "DateTime.toStr" },
            { nameof(DateOnly), "Date.toStr" },
            { nameof(DateOnly), "Date.toStr" },
            { nameof(Int32), "" },
            { nameof(String), "" },
        };
        public string handleToJson(TsTypeInf type)
        {
            return map2[type.name];
        }
        public void toJson(string name, string fullname, List<PropInf> props)
        {
            using (var hed = this.newFunction("@override\n Map<String, dynamic> toJson", new List<Tuple<string, string>>() { }, name)) //TODO isclientCreatble or not 
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

                hed.WriteLine($"final data = super.toJson();");
                hed.WriteLine($" data[\"\\$type\"]=\"{fullname}\"");
                foreach(var pr in props.Where(x=> !x.fromSuper))
                {
                    //map["EnterDate"] = enterDate.toString();
                    if (pr.type.nullable)
                        hed.WriteLine($"data['{pr.name}'] = {pr.name} != null ? {handleToJson(pr.type)}('{pr.name}') : null,");
                    else
                        hed.WriteLine($"{pr.name} = DateTime.parse(json['{pr.name}'])");
                }
                hed.WriteLine($"return data;");

            }
        }
        public void fromJson(string name, string fullname, List<PropInf> props)
        {
            this.WriteLine("{name}.fromJson(Map<String, dynamic> json):"); //TODO isclientCreatble or not 
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

                this.WriteLine($"super.fromJson(json);");
                foreach (var pr in props.Where(x => !x.fromSuper))
                {
                    //enterDate = json['EnterDate'] != null ? DateTime.parse(json['EnterDate']) : DateTime.now(),
                    if (pr.type.nullable)
                        this.WriteLine($"{pr.name} = json['{pr.name}'] != null ? {handleFromJson(pr.type)}(json['{pr.name}']) : null,");
                    else
                        this.WriteLine($"{pr.name} = DateTime.parse(json['{pr.name}']),");
                }
                this.WriteLine($";");

            }
        }


    }

}
