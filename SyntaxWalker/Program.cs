using static System.Console;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using System;
using Microsoft.Build.Locator;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static SyntaxWalker.Program;
using System.Runtime;
using System.Reflection.Metadata;
using System.Buffers.Text;
using System.Text.RegularExpressions;
using System.Data.SqlTypes;
using System.Collections;



//using Microsoft.CodeAnalysis.Common;
namespace SyntaxWalker
{
    partial class Program
    {

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }
            return visualStudioInstances[0];//visualStudioInstances.Length - 1];
            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }
        static IEnumerable<FileInfo> SearchDirectory(DirectoryInfo directory, int deep = 0)
        {


            foreach (var subdirectory in directory.GetDirectories())
                foreach (var item in SearchDirectory(subdirectory, deep + 1))
                    yield return item;

            foreach (var file in directory.GetFiles())
                yield return file;
        }

        public static Dictionary<string, string> tsMap = new() { { "int", "Number" },{ "float","Number"} ,
         { "Int32", "Number" },
         { "Int64", "Number" },
         { "Decimal", "Number" },
        { "long", "Number" },
        { "Single", "Number" },
            { "DateTimeOffset","Date"},
            { "DateTime","Date"}
        };
       
        public static Dictionary<ITypeSymbol, TypeDes> managerMap = new() { };
        public static Dictionary<TypeInfo, TypeDes> managerMap2 = new() { };
        public class TsTypeInf
        {

        }
        private static string getTsName(string type)
        {
            var x = type;
            if (x.EndsWith('?'))
            {
                x = x.Substring(0, x.Length - 1);
                return getTsName(x) + " | undefined";
            }
            string res;
            if (tsMap.TryGetValue(type, out res))
                return res;

            return type;
        }
        private static string getTsName(TypeInfo type, SemanticModel sm)
        {



            return getTsName(type.Type, sm);
        }
        private static string getTsName(ITypeSymbol type, SemanticModel sm)
        {
            //return "TODO";
            //var tt=sm.GetTypeInfo(type);
            if (type.OriginalDefinition.Name == "Nullable")
            {

                Console.WriteLine("");
                var s = type as INamedTypeSymbol;
                return getTsName(s.TypeArguments.FirstOrDefault(), sm) + " | undefined";
                Console.WriteLine("");
            }
            if (type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {

                Console.WriteLine("");
                var res = getTsName(type.Name) + "<";

                res += s2.TypeArguments.ToList().ConvertAll(x => getTsName(x, sm)).Aggregate((l, r) => $"{l},{r}");
                res += ">";
                return res;
            }
            return getTsName(type.Name);

        }
        private static string getTsName(TypeSyntax type, SemanticModel sm)
        {

            var tt = sm.GetTypeInfo(type);
            return getTsName(tt, sm);
        }

        private static List<ITypeSymbol> GetAllNames(ITypeSymbol type)
        {
            var res = new List<ITypeSymbol>();
            //var tt = sm.GetTypeInfo(type0);
            //var type = tt.Type;
            res.Add(type);

            if (type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {
                foreach (var x in s2.TypeArguments)
                    res.AddRange(GetAllNames(x));

                return res;
            }
            return res;
        }
        private static void addAllNames(ITypeSymbol type, IBlockDespose wr)
        {
            foreach (var t in GetAllNames(type))
            {
                wr.addUsedType(t);
            }
        }


        private static TypeDes addOrUpdateManager(ITypeSymbol type0,TypeInfo? type=null, ITypeSymbol keyType=null, string fn=null,IEnumerable<ITypeSymbol> used=null)
        {
            TypeDes res;
            if (!managerMap.TryGetValue(type0, out res))
                managerMap[type0] = res = new TypeDes();
            if(type!=null)
                managerMap[type0].type = type.Value;
            if (fn != null)
                res.fn = fn;
            if (keyType != null)
                res.keyType = keyType;
            if (used != null)
                foreach (var u in used)
                    res.usedTypes.Add(u);
            return res;
        }
        public static IEnumerable<TypeInfo> getBases(TypeDeclarationSyntax class_, SemanticModel sm)
        {

            return class_.BaseList?.Types.Select(x =>
            { //x.ToString()
                return sm.GetTypeInfo(x.Type);
                //return z.Type;
            });
        }


        public static List<ITypeSymbol> getInterfaces(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            var res = new List<ITypeSymbol>();
            if (class_.BaseList != null)
            {

                foreach (var base_ in class_.BaseList.Types)
                {

                    var rmp2 = sm.GetTypeInfo(base_.Type);
                    
                    if (rmp2.Type.TypeKind == TypeKind.Interface)
                        res.Add(rmp2.Type);

                }
            }
            return res;


        }
        public static string getHeader(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            var interfaces = getInterfaces(class_, tt2, sm);
            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, null, null, interfaces);
            if (interfaces.Count() > 0)
                return $" extends {interfaces.ConvertAll(x => getTsName(x, sm)).Aggregate((l, r) => $"{l},{r}")}";
            return "";

        }
        public static string getHeaderClass(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            string res = "";


            var baseClass = class_.getBaseClass(sm);

            var interfaces = getInterfaces(class_, tt2, sm);
            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, null, null, interfaces);
            if (baseClass != null)
                res += $" extends {getTsName(baseClass, sm)}";
            if (interfaces.Count() > 0)
                res += $" implements {interfaces.ConvertAll(x => getTsName(x, sm)).Aggregate((l, r) => $"{l},{r}")}";
            return res;
        }
        public static PropertyDeclarationSyntax getPropWithName(TypeDeclarationSyntax class_, string name, SemanticModel sm)
        {
            //var h = class_.getBaseClass(sm);
            //var uu = h.GetMembers().Where(x => x.Name == name).FirstOrDefault();
            return class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Identifier.ToString() == name).First();
        }
        /*public static PropertyDeclarationSyntax getPropWithNameFinal(TypeDeclarationSyntax class_, string name, SemanticModel sm)
        {
            
            var res= class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Identifier.ToString() == name).First();
            if (res != null)
                return res;
            var h = class_.getBaseClass(sm);
            h.

        }*/
        /*public static ISymbol getPropWithName2(TypeDeclarationSyntax class_, string name, SemanticModel sm)
        {
            var res = sm.GetDeclaredSymbol(class_).GetMembers().Where(x => x.Name == name).FirstOrDefault();
            if (res != null)
                return res;

            var h = class_.getBaseClass(sm);
            if (h != null)
            {
                return getPropWithName2(h, name, sm);
                var uu = h.GetMembers().Where(x => x.Name == name).FirstOrDefault();
                if (uu != null)
                    return uu;
            }
            //var idMemeber = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Identifier.ToString() == name).First();
           
            //var tt = sm.GetTypeInfo(idMemeber.Type);
            //return tt.Type as INamedTypeSymbol;
        }*/
        public static ISymbol getPropWithName3(INamedTypeSymbol class_, string name)
        {
            var res = class_.GetMembers().Where(x => x.Name == name).FirstOrDefault();
            if (res != null)
                return res;
            if (class_.BaseType != null)
                return getPropWithName3(class_.BaseType, name);
            return null;
        }
        public static ISymbol getPropWithName4(TypeInfo class_, string name)
        {
            var res = class_.Type.GetMembers().Where(x => x.Name == name).FirstOrDefault();
            if (res != null)
                return res;

            return null;
        }
      
        
        public static Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax> findForgienKeyMen2(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var class_2 = sm.GetDeclaredSymbol(class_);
            var class3 = sm.GetTypeInfo(class_);
            var res = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>();
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                foreach (var mem in mems)
                {
                    var mem2 = mem;
                    var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();

                    if (anot.Where(x => x.Name.ToString() == "ForeignKey").Any())
                    {
                        var z = anot.Where(x => x.Name.ToString() == "ForeignKey").First();
                        var tz = z.ArgumentList.Arguments.First();

                        {

                            var rmp2 = sm.GetConstantValue(tz.Expression);

                            var idMemeber = getPropWithName(class_, rmp2.ToString(), sm);
                            //idMemeber.Identifier.ToString()
                            //var idMemeber = getPropWithName3(class_2, rmp2.ToString());
                            {
                                var type = sm.GetTypeInfo(idMemeber.Type).Type;

                                //var type = idMemeber.Type;


                                Console.WriteLine("");

                                mem.Type.GetNamespace();




                                //var resName = sm.GetTypeInfo(getPropWithName(class_, rmp2.ToString(),sm).Type).Type;// getPropWithName(class_, rmp2.ToString()).Type.GetFullName();
                                var memType = sm.GetTypeInfo(mem.Type); //sm.GetTypeInfo(mem.Type).GetFullName();
                                var idType = sm.GetTypeInfo(idMemeber.Type);
                                var memTypeS = memType.Type.ToString();

                                if (memTypeS == "int" || memTypeS == "string" || memTypeS == "System.Guid"
                                    || memTypeS == "Guid"
                                    || memTypeS == "Guid?"
                                    || memTypeS == "int?" || memTypeS == "string?" || memTypeS == "System.Guid?"
                                    || memType.Type.TypeKind == TypeKind.Struct
                                    )
                                {
                                    var tmp = memType;
                                    memType = idType;
                                    idType = tmp;

                                    var t2 = mem;
                                    mem2 = idMemeber;
                                    idMemeber = t2;
                                }
                                ITypeSymbol s = idType.Type as INamedTypeSymbol;
                                if (idType.Type.OriginalDefinition.Name == "Nullable")
                                {



                                    s = (s as INamedTypeSymbol).TypeArguments.FirstOrDefault();

                                }
                                var anot2 = idMemeber.DescendantNodes().OfType<AttributeSyntax>().ToList();
                                if (anot2.Where(x => x.Name.ToString() == "JsonIgnore").Any())
                                    continue;
                                res[idMemeber] = mem2;
                                

                            }


                        }

                    }
                }
            }
            return res;


        }

        public static List<PropertyDeclarationSyntax> getCollections(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var res = new List<PropertyDeclarationSyntax>();

            
            var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
            foreach (var mem in mems)
            {

                var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();

                if (mem.Type is GenericNameSyntax genericNameSyntax)
                {
                    var nd = genericNameSyntax.ChildNodes();
                    if (genericNameSyntax.Identifier.ToString() == "ICollection")
                    {
                        var nd2 = genericNameSyntax.TypeArgumentList.Arguments.ToList()[0];
                        res.Add(mem);
                    }
                }
            }   
            
            return res;

        }
        public static List<PropertyDeclarationSyntax> getSadeProp(TypeDeclarationSyntax class_, SemanticModel sm)
        {
            var frs = findForgienKeyMen2(class_, sm);
            var cols = getCollections(class_, sm);
            var res = new List<PropertyDeclarationSyntax>();
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                foreach (var mem in mems)
                {
                    var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();
                    if (anot.Where(x => x.Name.ToString() == "JsonIgnore").Any())
                        continue;
                    if (cols.Contains(mem))
                        continue;
                    if (frs.Values.Where(x => x == mem).Any())
                        continue;
                    if (frs.Keys.Where(x => x == mem).Any())
                        continue;
                    res.Add(mem);
                }
            }
            return res;
        }
        public static List<PropertyDeclarationSyntax> handleTypeMemeber(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm, bool isClass)
        {
            var classType=sm.GetTypeInfo(class_);
            //classType.Type.GetMembers().OfType<IPropertySymbol>();
            var res = new List<PropertyDeclarationSyntax>();

            var frs = findForgienKeyMen2(class_, sm);
            var cols = getCollections(class_, sm);
            var fields = getSadeProp(class_, sm);
            foreach(var f in fields)
            {
                var rmp2 = sm.GetTypeInfo(f.Type);
                if (f.Type is GenericNameSyntax gns)
                {
                    foreach(var ta in gns.TypeArgumentList.Arguments)
                    {
                        var s=sm.GetTypeInfo(ta);
                        //addAllNames(s.Type, tt2);
                        addOrUpdateManager(sm.GetDeclaredSymbol(class_), null, null, null, new List<ITypeSymbol>() { s.Type });
                        Console.WriteLine(ta);
                    }
                }
                tt2.WriteLine($"{toCamel(f.Identifier.ToString())} : {getTsName(rmp2.Type, sm)};");
                res.Add(f);
            }
            foreach (var f in frs)
            {

                var rmp2 = sm.GetTypeInfo(f.Key.Type);
                var rmp22 = sm.GetTypeInfo(f.Value.Type);
                var nullable = rmp2.Type.OriginalDefinition.Name == "Nullable";
                var nullableS = nullable ? "?" : "";
                //tt2.WriteLine($"{toCamel(f.Key.Identifier.ToString())} : {getTsName(rmp2.Type, sm)};");
                tt2.WriteLine($"{toCamel(f.Key.Identifier.ToString())}{nullableS} : Forg<{rmp22.Type.Name},{getTsName(rmp2.Type,sm)}>;");
                res.Add(f.Key);
            }
            foreach (var f in frs)
            {
                var rmp2 = sm.GetTypeInfo(f.Key.Type);
                var rmp22 = sm.GetTypeInfo(f.Value.Type);
                //addAllNames(rmp22.Type, tt2);
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), null, null, null, new List<ITypeSymbol>() { rmp22.Type });
                var nullable = rmp2.Type.OriginalDefinition.Name == "Nullable";
                var nullableS = nullable ? "!" : "";
                using (var wr3 = tt2.newFunction($"get{f.Value.Identifier.ToString()}",null,$"Promise<{rmp22.Type.Name}>",true))
                {
                    wr3.WriteLine("//this code must handle async and sync 2");

                    wr3.WriteLine($" return await {rmp22.Type.Name}Manager.get(this.{toCamel(f.Key.Identifier.ToString())}{nullableS});");

                    //addOrUpdateManager(mem.Type.ToString(), getPropWithName(class_, rmp3.ToString()).Type.ToString(), null).isResource = true;
                }
            }
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                foreach (var mem in mems)
                {

                    var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();

                    if (mem.Type is GenericNameSyntax genericNameSyntax)
                    {
                        var nd = genericNameSyntax.ChildNodes();
                        if (genericNameSyntax.Identifier.ToString() == "ICollection")
                        {
                            var nd2 = genericNameSyntax.TypeArgumentList.Arguments.ToList()[0];
                            using (var wr2 = tt2.newBlock($"async get{mem.Identifier.ToString()}():Promise<{nd2}[]>"))
                            {

                                var rmp3 = sm.GetTypeInfo(nd2);
                                //addAllNames(rmp3.Type, wr2);
                                addOrUpdateManager(sm.GetDeclaredSymbol(class_), null, null, null, new List<ITypeSymbol>() { rmp3.Type });

                                wr2.WriteLine("//this code must be handle catch");
                                var url = (class_.Parent as NamespaceDeclarationSyntax).Name + "." + class_.Identifier;
                                url = url.Replace(".", "__");

                                wr2.WriteLine($" var ar =await httpGettr.Get<{nd2}>(\"v1/generico/{url}/\"+this.id+\"/{mem.Identifier.ToString()}\")!;");
                                wr2.WriteLine($" // TODO {nd2}Manager.update(ar);");
                                wr2.WriteLine($" return  ar;");
                            }

                        }
                        continue;
                    }
                    var rmp2 = sm.GetTypeInfo(mem.Type);
                    //frs.Values.Where(x=> x.enityFildNameName== mem.Identifier.ToString()).Any()
                   
                    

                    //Console.WriteLine(mem.Type);
                    //Console.WriteLine(mem.Identifier.ToString());
                    addOrUpdateManager(sm.GetDeclaredSymbol(class_), null, null, null, new List<ITypeSymbol>() { rmp2.Type });

                    

                    
                  


                }
                
            }
            return res;

        }


        public static void handleType(TypeDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {


            
            using (var tt2 = tt.newBlock($"export interface {GetName(class_)} {getHeader(class_, tt, sm)} "))
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                handleTypeMemeber(class_, tt2, sm, false);
            }

        }
        public static void handleEnums(EnumDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {

            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            addOrUpdateManager(sm.GetDeclaredSymbol(class_),memType0, null, tt.getFileName());
            using (var tt2 = tt.newBlock($"export enum {class_.Identifier} "))
            {
                foreach (var mem in class_.Members)
                {
                    //tt.WriteLine($"{mem.Identifier}");


                    var v = mem.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                    if (v != null)
                    {
                        var rmp2 = sm.GetConstantValue(v.Value);


                        //v.DescendantNodes().OfType<LiteralExpressionSyntax>();
                        tt.WriteLine($"{mem.Identifier} = {rmp2},");

                    }
                    else
                    {
                        tt.WriteLine($"{mem.Identifier} ,");
                    }

                }


                //.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
            }


        }
        public static void handleInterface(InterfaceDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {
            var memType0 = sm.GetTypeInfo(class_);// sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(sm.GetDeclaredSymbol(class_),memType0, null, tt.getFileName());

            handleType(class_, tt, sm);

        }
        public static IEnumerable<IPropertySymbol> getProps(ITypeSymbol h)
        {
            
            var z = h.GetMembers().OfType<IPropertySymbol>().Where(x => !x.GetAttributes().Any(y => y.AttributeClass.Name == "JsonIgnoreAttribute"));
            return z;
        }
        public static string GetName(TypeDeclarationSyntax class_)
        {
            var res = "";
            if (class_.TypeParameterList != null && class_.TypeParameterList.ChildNodes().ToList().Count() > 0)
                res = $"<{class_.TypeParameterList.ChildNodes().ToList().ConvertAll(x => x.ToString()).Aggregate((l, r) => $"{l},{r}")}>";
            return $"{class_.Identifier.ToString()}{res}";
        }
        public static void handleClass(ClassDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {
            //var type=class_.TypeParameterList
            /*if (type.Type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {

                Console.WriteLine("");
                var res = getTsName(type.Type.Name) + "<";

                res += s2.TypeArguments.ToList().ConvertAll(x => getTsName(x)).Aggregate((l, r) => $"{l},{r}");
                res += ">";
                return res;
            }*/
            string fullname = class_.GetNamespace();

            var classType = sm.GetTypeInfo(class_);// sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(sm.GetDeclaredSymbol(class_),classType, null, tt.getFileName());



            var superClassSymbol = class_.getBaseClass(sm);
            var hh = class_.GetBaseClasses(sm);
            if (superClassSymbol != null)
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), used:new List<ITypeSymbol>() { superClassSymbol });


            //if(h!= null) 
            //    tt.WriteLine($"@Deserialize.inheritSerialization(() => {getTsName(h,sm)})");
            using (var tt2 = tt.newClass($"export class {GetName(class_)}  {getHeaderClass(class_, tt, sm)} "))
            {
                //var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                var res = handleTypeMemeber(class_, tt2, sm, true);
                //managerMap[classType.Type].filds = res;
                //var z = classType.Type.GetMembers().OfType<IPropertySymbol>().Where(x => !x.GetAttributes().Any(y => y.AttributeClass.Name == "JsonIgnoreAttribute"));

                var args =new List<Tuple<string, string>>();
                //if (superClassSymbol != null)
                foreach(var superClassSymbol1 in hh)
                {
                    args.AddRange(getProps(superClassSymbol1).ToList().ConvertAll(x => new Tuple<string, string>(x.Name, getTsName(x.Type, sm))));
                    //hh.AddRange(getProps(h).ToList().ConvertAll(x => $" {x.Name}:{getTsName(x.Type,sm)}"));
                }
                args.AddRange(res.ConvertAll(x => new Tuple<string, string>(x.Identifier.ToString(), getTsName(sm.GetTypeInfo(x.Type).Type, sm))));
                //args.AddRange(z.ToList().ConvertAll(x => new Tuple<string, string>(x.Name, getTsName(x.Type, sm))));

                using (var hed = tt2.newConstructor(args))
                {

                    {
                        if (superClassSymbol != null)
                        {
                            //getProps(superClassSymbol)?.ToList().ConvertAll(x => x.Name)
                            hed.SuperCunstrocotrCall(new() { "args" });
                        }
                        foreach (var m in res)
                            hed.WriteLine($"this.{toCamel(m.Identifier.ToString())} = args.{m.Identifier.ToString()};");
                    }
                }
            }




        }
        public static void handleStruct(StructDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {
            //var type=class_.TypeParameterList
            /*if (type.Type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {

                Console.WriteLine("");
                var res = getTsName(type.Type.Name) + "<";

                res += s2.TypeArguments.ToList().ConvertAll(x => getTsName(x)).Aggregate((l, r) => $"{l},{r}");
                res += ">";
                return res;
            }*/
            string fullname = class_.GetNamespace();

            var classType = sm.GetTypeInfo(class_);// sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), classType, null, tt.getFileName());



            
            //if(h!= null) 
            //    tt.WriteLine($"@Deserialize.inheritSerialization(() => {getTsName(h,sm)})");
            using (var tt2 = tt.newBlock($"export type {GetName(class_)} = "))
            {
                //var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                var res = handleTypeMemeber(class_, tt2, sm, true);
                //managerMap[classType.Type].filds = res;
                //var z = classType.Type.GetMembers().OfType<IPropertySymbol>().Where(x => !x.GetAttributes().Any(y => y.AttributeClass.Name == "JsonIgnoreAttribute"));

                var args = new List<Tuple<string, string>>();
                //if (superClassSymbol != null)
               
                args.AddRange(res.ConvertAll(x => new Tuple<string, string>(x.Identifier.ToString(), getTsName(sm.GetTypeInfo(x.Type).Type, sm))));
                //args.AddRange(z.ToList().ConvertAll(x => new Tuple<string, string>(x.Name, getTsName(x.Type, sm))));
                if(false)
                using (var hed = tt2.newConstructor(args))
                {






                    {
                        
                        foreach (var m in res)
                            hed.WriteLine($"this.{toCamel(m.Identifier.ToString())} = args.{m.Identifier.ToString()};");
                    }
                }
            }




        }

        public static Dictionary<Project, Compilation> comp = new ();
        public static Dictionary<SyntaxTree, SemanticModel> smC = new ();
        public static Dictionary<SyntaxTree, CompilationUnitSyntax>  rootC = new ();
        static async Task Main(string[] args)
        {
            var rgx = new Regex(@"Models/Basics/*");
            Console.WriteLine(rgx.Match("Models/Basics/adf").Success);
            Console.WriteLine(rgx.Match("Models/Basics/adf/sdfs").Success);

            //var wr = new Writer() { writer = new FileWriter("D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\Models2.ts") };
            //var wr = new Writer() { writer = new FileWriter("D:\\programing\\Models2.ts") };

            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);

            var map = new HashSet<string>();
            var prjfn = "D:\\programing\\TestHelperTotal\\TestHelperServer2\\TestHelper2.sln";
            var fns = new List<FileBlock>();
            using (var workspace = MSBuildWorkspace.Create())
            {


                //var solution = await workspace.OpenSolutionAsync("D:\\programing\\trade\\StockSharp\\StockSharp.sln");
                var solution = await workspace.OpenSolutionAsync(prjfn);
                //var solution = await workspace.OpenSolutionAsync("D:\\programing\\cs\\TestForAnalys\\ConsoleApp1\\ConsoleApp1.sln");// D:\\programing\\trade\\StockSharp\\StockSharp.sln");
                
                
                foreach (var project in solution.Projects)
                {
                    //if (project.Name != "old_Models" && project.Name != "ViewGeneratorBase" && project.Name != "Models" && project.Name != "BaseModels")
                    //    continue;
                    var compilation = await project.GetCompilationAsync();
                    comp[project] = compilation;
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                        rootC[tree] = root;
                        SemanticModel model = compilation.GetSemanticModel(tree);
                        smC[tree] = model;
                    }
                }
                for (int ji = 0; ji < 100; ++ji)
                    try
                    {
                        managerMap.Clear();
                        fns.Clear();

                        foreach (var project in solution.Projects)
                        {
                            if (project.Name != "old_Models" && project.Name != "ViewGeneratorBase" && project.Name != "Models" && project.Name != "BaseModels")
                                continue;
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            foreach (var tree in compilation.SyntaxTrees)
                            {
                                CompilationUnitSyntax root = rootC[tree];// tree.GetCompilationUnitRoot();
                                SemanticModel model = smC[tree];// compilation.GetSemanticModel(tree);
                                 
                                // </Snippet2>

                                // <Snippet6>
                                /*string s = root.FilePath;
                                if (!s.Contains("Models\\Models"))
                                    continue;/**/

                                //var wr = new Writer() { writer=new FileWriter("D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models2.ts") };
                                var fi = new FileInfo(prjfn);

                                var fn2 = tree.FilePath.Substring(fi.Directory.FullName.Length + 1, tree.FilePath.Length - fi.Directory.FullName.Length - 4);
                                //using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\{fn2}.ts"))
                                {

                                    var wr = new FileBlock(null,null,0) { fn = fn2 };
                                    fns.Add(wr);

                                    var nameSpaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

                                    foreach (var namespace_ in nameSpaces)
                                    {
                                        var tt = wr;
                                        //using (var tt = wr.newNameSpace(namespace_.Name.ToString()))
                                        {
                                            var enums = namespace_.DescendantNodes().OfType<EnumDeclarationSyntax>();
                                            foreach (var interface_ in enums)
                                                try
                                                {
                                                    
                                                    handleEnums(interface_,
                                                                wr,
                                                                model);
                                                }
                                                catch
                                                {

                                                }


                                            var interfaces = namespace_.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
                                            foreach (var interface_ in interfaces)
                                                try
                                                {
                                                    handleInterface(interface_,
                                                                wr,
                                                                model);
                                                }
                                                catch
                                                {

                                                }

                                            var classes = namespace_.DescendantNodes().OfType<ClassDeclarationSyntax>();
                                            foreach (var class_ in classes)
                                                try
                                                {
                                                    // wr.WriteLine("Deserialize.RuntimeTypingEnable();");

                                                    handleClass(class_,
                                                                wr,
                                                                model);
                                                    //wr.WriteLine($"Deserialize.RuntimeTypingSetTypeString({GetName(class_)}, \"{GetName(class_)}\");");


                                                }
                                                catch
                                                {

                                                }
                                            var structs = namespace_.DescendantNodes().OfType<StructDeclarationSyntax>();
                                            foreach (var class_ in structs)
                                                try
                                                {
                                                    // wr.WriteLine("Deserialize.RuntimeTypingEnable();");

                                                    handleStruct(class_,
                                                                wr,
                                                                model);
                                                    //wr.WriteLine($"Deserialize.RuntimeTypingSetTypeString({GetName(class_)}, \"{GetName(class_)}\");");


                                                }
                                                catch
                                                {

                                                }
                                        }
                                    }
                                    for (int i = 0; i < 200; ++i)
                                        wr.WriteLine("                                                                                      ");

                                    wr.WriteLine("                                     ");

                                    //fwriter.Flush();
                                }


        ;
                            }


                        }


                        {
                            var project=solution.Projects.First(x => x.Name == "Data");
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            var ress=getContext(project, compilation);

                        }
                        {
                            var project = solution.Projects.First(x => x.Name == "old_Data");
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            var ress = getContext(project, compilation);

                        }
                        var pr = managerMap.Values.ToList();
                        for (int ti = 0; ti < pr.Count(); ++ti)
                            try
                            {
                                var t = pr[ti];
                                if (true)
                                {
                                    var ff = fns.Where(x => x.fn == t.fn).FirstOrDefault();
                                    if (ff.lines.Count > 1)
                                    {
                                        try
                                        {
                                            using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\{ff.fn}.ts"))
                                            {
                                                fwriter.WriteLine("import  { Guid,Forg,httpGettr,List,ForeignKey,ForeignKey2,Rial } from \"Models/base\";");
                                                fwriter.WriteLine("import { IIdMapper , IdMapper} from \"Models/Basics/BaseModels/Basics/Basics\"");
                                                //fwriter.WriteLine("import * as Deserialize from \"dcerialize\"");

                                                //fwriter.WriteLine("import * as Deserialize from 'dcerialize'");

                                                fwriter.WriteLine("\n\n");
                                                foreach (var tf in ff.usedTypes)
                                                {
                                                    if (!managerMap.ContainsKey(tf))
                                                        continue;

                                                    if (managerMap[tf].fn != null && managerMap[tf].fn != ff.fn)
                                                    {
                                                        fwriter.WriteLine($"import {{ {tf.Name} }} from \"Models/{linuxPathStyle(managerMap[tf].fn)}\"");
                                                        if (!pr.Contains(managerMap[tf]))
                                                            pr.Add(managerMap[tf]);
                                                    }

                                                }
                                                fwriter.WriteLine("\n\n");
                                                foreach (var tf in ff.usedTypes)
                                                {
                                                    if (!managerMap.ContainsKey(tf))
                                                        continue;
                                                    if (managerMap[tf].isResource)
                                                        if(managerMap[tf].isOld)
                                                            fwriter.WriteLine($"import {{ {tf.Name}Manager }} from \"Models/oldManagers\"");
                                                        else
                                                            fwriter.WriteLine($"import {{ {tf.Name}Manager }} from \"Models/managers\"");

                                                }
                                                fwriter.WriteLine("\n\n");
                                                foreach (var l in ff.lines)
                                                    fwriter.WriteLine(l.ToString());

                                                //Deserialize.RuntimeTypingSetTypeString(EntityList<CoachType>, "Models.EntityList<Coach>");

                                                fwriter.Flush();
                                            }
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }

                            }
                            catch { }
                        if(false)
                        using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\oldManagers.ts"))
                        {
                            fwriter.WriteLine("import { Guid, Forg,httpGettr } from \"./base\";");
                            fwriter.WriteLine("import { IEntityManager,EntityManager } from \"./baseManagers\";");
                            fwriter.WriteLine("\n\n");
                            foreach (var t in managerMap)
                                if (t.Value.isResource && t.Value.fn.StartsWith("old_testhelper"))
                                {
                                    fwriter.WriteLine($"import {{ {t.Key.Name} }} from \"Models/{linuxPathStyle(t.Value.fn)}\"");
                                }


                            fwriter.WriteLine("\n\n");
                            foreach (var t in managerMap)
                                if (t.Value.isResource && t.Value.fn.StartsWith("old_testhelper"))
                                {
                                    fwriter.WriteLine($"export var {t.Key.Name}Manager=new EntityManager<{t.Key.Name},Number>(\"{t.Key}\");");
                                }
                        }
                        using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\managers.ts"))
                        {
                            fwriter.WriteLine("import { Guid, Forg,httpGettr } from \"./base\";");
                            fwriter.WriteLine("import { IEntityManager,EntityManager } from \"./baseManagers\";");
                            fwriter.WriteLine("\n\n");
                            foreach (var t in managerMap)
                                if (t.Value.isResource && t.Value.fn.StartsWith("Models"))
                                {
                                    fwriter.WriteLine($"import {{ {t.Key.Name} }} from \"Models/{linuxPathStyle(t.Value.fn)}\"");
                                }


                            fwriter.WriteLine("\n\n");
                            foreach (var t in managerMap)
                                if (t.Value.isResource && t.Value.fn.StartsWith("Models"))
                                {
                                    fwriter.WriteLine($"export var {t.Key.Name}Manager=new EntityManager<{t.Key.Name},Number>(\"{t.Key}\");");
                                }
                        }

                    }
                    catch (Exception e)
                    {

                    }

            }
            var pw = new ProjectWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\", "ts");
            for (int i = 0; i < 100; ++i)
            {
                var fls = new List<Regex>();
                //D:\programing\TestHelperTotal\TestHelperWebsite\src\Models\Basics\BaseModels\Basics\Basics.ts
                fls.Add(new Regex(@"Basics/BaseModels/Basics/Basics"));
                //fls.Add(new Regex(@"Basics/BaseModels/*"));
                fls.Add(new Regex(@"old_testhelper/Models/Models/*"));
                fls.Add(new Regex(@"old_testhelper/ViewGeneratorBase/[A-Za-z]*"));

                foreach (var ff in fns)
                    if (ff.lines.Count > 1 && fls.Any(x => x.Match(ff.fn).Success))
                    {
                        using (var fwriter = pw.getFile($"src\\Models\\{ff.fn}"))
                        {

                            fwriter.WriteHeader();
                            //fwriter.WriteLine("import * as Deserialize from 'dcerialize'");


                            foreach (var t in ff.usedTypes)
                            {
                                if (!managerMap.ContainsKey(t))
                                    continue;
                                if (managerMap[t].fn != ff.fn)
                                {
                                    fwriter.WriteLine($"import {{ {t} }} from \"Models/{linuxPathStyle(managerMap[t].fn)}\"");
                                    //fwriter.WriteImport($"import {{ {t} }} from \"Models/{linuxPathStyle(managerMap[t].fn)}\"");
                                }

                            }
                            fwriter.WriteLine("\n\n");
                            foreach (var t in ff.usedTypes)
                            {
                                if (!managerMap.ContainsKey(t))
                                    continue;
                                if (managerMap[t].isResource)
                                    fwriter.WriteLine($"import {{ {t.Name}Manager }} from \"Models/managers\"");

                            }
                            fwriter.WriteLine("\n\n");
                            foreach (var l in ff.lines)
                                fwriter.WriteLine(l.ToString());

                            //Deserialize.RuntimeTypingSetTypeString(EntityList<CoachType>, "Models.EntityList<Coach>");

                            fwriter.Flush();
                        }
                    }
           
            }

        }
        public static List<ITypeSymbol> getContext(Project project, Compilation compilation)
        {
            var res = new List<ITypeSymbol>();
            {
                for (int i = 0; i < 3; ++i)
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        CompilationUnitSyntax root = rootC[tree];//tree.GetCompilationUnitRoot();
                        SemanticModel model = smC[tree];//compilation.GetSemanticModel(tree);
                        var nameSpaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

                        foreach (var namespace_ in nameSpaces)
                        {
                            //var DBcontext = namespace_.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ToString() == "DBContext");
                            var DBcontext = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(x => x.getBaseClass(model)?.Name == "DbContext");
                            if (DBcontext == null)
                                continue;
                            var props = DBcontext.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(x => x.Type.Kind() == SyntaxKind.GenericName
                            && (x.Type as GenericNameSyntax).Identifier.ToString() == "DbSet").Select(x => x.Type).OfType<GenericNameSyntax>();

                            foreach (var pr3 in props)
                            {
                                var nd2 = pr3.TypeArgumentList.Arguments.ToList()[0];
                                Console.WriteLine("dbset");
                                var type = model.GetTypeInfo(nd2);
                                var key2 = managerMap.Keys.Where(x => x.ToString() == type.Type.ToString()).FirstOrDefault();
                                if (key2 != null && managerMap.ContainsKey(key2)) try
                                    {
                                        res.Add(key2);
                                    }
                                    catch
                                    {

                                    }
                            }


                        }
                    }
                return res;
            }

        }
        private static object linuxPathStyle(string fn)
        {
            return fn.Replace('\\', '/');
        }
        public static string toCamel(string name)
        {
            return Char.ToLowerInvariant(name[0]) + name.Substring(1);

        }
}
}
