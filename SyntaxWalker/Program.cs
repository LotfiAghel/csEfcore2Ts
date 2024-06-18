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
using System.Text.RegularExpressions;




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

        static List<FileBlock> fns = new();
        public static Dictionary<ITypeSymbol, TypeDes> managerMap = new() { };
        public static Dictionary<TypeInfo, TypeDes> managerMap2 = new() { };
       
        private static TsTypeInf getTsName(string type)
        {
            var x = new TsTypeInf(type);
            if (x.name.EndsWith('?'))
            {
                x.name = x.name.Substring(0, x.name.Length - 1);
                var z = getTsName(x.name);
                z.nullable = true;
                return  z;
            }
            TsTypeInf res;
            if (tsMap.TryGetValue(type, out res))
                return res;

            return x;
        }
        private static TsTypeInf getTsName(TypeInfo type, SemanticModel sm)
        {



            return getTsName(type.Type, sm);
        }
        private static TsTypeInf getTsName(ITypeSymbol type, SemanticModel sm)
        {
            //return "TODO";
            //var tt=sm.GetTypeInfo(type);
            if (type.OriginalDefinition.Name == "Nullable")
            {

                Console.WriteLine("");
                var s = type as INamedTypeSymbol;
                var z= getTsName(s.TypeArguments.FirstOrDefault(), sm);
                z.nullable = true;
                return z;
                Console.WriteLine("");
            }
            if (type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {

                Console.WriteLine("");
                var res = getTsName(type.Name);
                res.name+= "<";

                res.name += s2.TypeArguments.ToList().ConvertAll(x => getTsName(x, sm).name).Aggregate((l, r) => $"{l},{r}");
                res.name += ">";
                return res;
            }
            return getTsName(type.Name);

        }
        private static TsTypeInf getTsName(TypeSyntax type, SemanticModel sm)
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

        private static bool setIsHideFalse(ITypeSymbol z, HashSet<ITypeSymbol> mark)
        {
            if (mark.Contains(z) || !managerMap.ContainsKey(z))
                return true;
            mark.Add(z);
            var t = managerMap[z];
            t.isHide = false;
            foreach (var zz in t.usedTypes)
                setIsHideFalse(zz, mark);
            return true;
        }
        private static List<KeyValuePair<ITypeSymbol, TypeDes>> setIsHideFalseForDerivedClass(ITypeSymbol z)
        {
            var res = new List<KeyValuePair<ITypeSymbol, TypeDes>>();
            foreach(var e in managerMap)
            {
                if (e.Value.syntax == null)
                    continue;
                var bases = e.Value.syntax.GetBaseClasses(e.Value.sm);
                if (bases.Contains(z))
                {
                    setIsHideFalse(e.Key, new ());
                    //e.Value.isHide = false;
                    res.Add(e);
                }
            }
            return res;
        }
        private static TypeDes addOrUpdateManager(ITypeSymbol type0, TypeInfo? type = null, ITypeSymbol keyType = null, string fn = null, ClassBlock block = null, IEnumerable<ITypeSymbol> used = null,
            TypeDeclarationSyntax syntax = null,
            SemanticModel sm = null,
            bool? isNonAbstractClass = null)
        {
            
            TypeDes res;
            if (!managerMap.TryGetValue(type0, out res))
                managerMap[type0] = res = new TypeDes();
            if (type != null)
                managerMap[type0].type = type.Value;
            if (fn != null)
                res.fn = fn;
            if (block != null)
                res.block = block;
            if (isNonAbstractClass != null)
                res.isNonAbstractClass = isNonAbstractClass.Value;
            if (syntax != null)
                res.syntax = syntax;
            if (sm != null)
                res.sm = sm;
            if (keyType != null)
            {
                res.keyType = keyType;
                res.keyTypeName = keyType.Name;
            }
            if (used != null)
                foreach (var u in used)
                {
                    res.usedTypes.Add(u);
                    if (u is INamedTypeSymbol un && un.IsGenericType)
                    {


                        Console.WriteLine("");
                        res.usedTypes.Add(un.OriginalDefinition);
                        foreach (var t in un.TypeArguments)
                            res.usedTypes.Add(t);
                    }
                }
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
            var bases=class_.GetBaseClasses(sm);
            var baseId=bases.Where(x => x.GetMembers().Any(x => x.Name == "id")).FirstOrDefault();
           
            if (interfaces.Count() > 0)
                return $" extends {interfaces.ConvertAll(x => getTsName(x, sm).name).Aggregate((l, r) => $"{l},{r}")}";
            return "";

        }
        public static string getHeaderClass(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            string res = "";


            var baseClass = class_.getBaseClass(sm);
            
            var interfaces = getInterfaces(class_, tt2, sm);
            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, used: interfaces,isNonAbstractClass:true);
            if (baseClass != null)
                res += $" extends {getTsName(baseClass, sm).name}";
            if (interfaces.Count() > 0)
                res += $" implements {interfaces.ConvertAll(x => getTsName(x, sm).name).Aggregate((l, r) => $"{l},{r}")}";
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
                    //if(mem.is==)
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
        public static List<PropertyDeclarationSyntax> handleTypeMemeber(TypeDeclarationSyntax class_, ClassBlock tt2, SemanticModel sm, bool isClass)
        {
            var classType = sm.GetTypeInfo(class_);
            //classType.Type.GetMembers().OfType<IPropertySymbol>();
            var res = new List<PropertyDeclarationSyntax>();

            var frs = findForgienKeyMen2(class_, sm);
            var cols = getCollections(class_, sm);
            var fields = getSadeProp(class_, sm);
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), block: tt2);
            foreach (var f in fields)
            {
                var rmp2 = sm.GetTypeInfo(f.Type);
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { rmp2.Type });
                if (f.Type is GenericNameSyntax gns)
                {
                    //addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { gns. });
                    foreach (var ta in gns.TypeArgumentList.Arguments)
                    {
                        var s = sm.GetTypeInfo(ta);
                        //addAllNames(s.Type, tt2);
                        addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { s.Type });
                        Console.WriteLine(ta);
                    }
                }
                var ttt = getTsName(rmp2.Type, sm);
                tt2.WriteLine($"{toCamel(f.Identifier.ToString())}{(ttt.nullable?"?":"")} : {ttt.name};");
                res.Add(f);
            }
            foreach (var f in frs)
            {

                var rmp2 = sm.GetTypeInfo(f.Key.Type);
                var rmp22 = sm.GetTypeInfo(f.Value.Type);
                var nullable = rmp2.Type.OriginalDefinition.Name == "Nullable";
                var nullableS = nullable ? "?" : "";
                //tt2.WriteLine($"{toCamel(f.Key.Identifier.ToString())} : {getTsName(rmp2.Type, sm)};");
                tt2.WriteLine($"{toCamel(f.Key.Identifier.ToString())}{nullableS} : Forg<{rmp22.Type.Name},{getTsName(rmp2.Type, sm).name}>;");
                res.Add(f.Key);
                //addOrUpdateManager(sm.GetDeclaredSymbol(class_), keyType: rmp2.Type);
            }
            foreach (var f in frs)
            {
                var rmp2 = sm.GetTypeInfo(f.Key.Type);
                var rmp22 = sm.GetTypeInfo(f.Value.Type);
                //addAllNames(rmp22.Type, tt2);
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { rmp22.Type });
                var nullable = rmp2.Type.OriginalDefinition.Name == "Nullable";
                var nullableS = nullable ? "!" : "";
                using (var wr3 = tt2.newFunction($"get{toCamelClass(f.Value.Identifier.ToString())}", null, $"Promise<{rmp22.Type.Name}>", true))
                {
                    wr3.WriteLine("//this code must handle async and sync 2");

                    wr3.WriteLine($" return await context.{rmp22.Type.Name}Manager.get(this.{toCamel(f.Key.Identifier.ToString())}{nullableS});");

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
                            var s = sm.GetTypeInfo(nd2);
                            addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { s.Type });

                            using (var wr2 = tt2.newBlock($"async get{toCamelClass(mem.Identifier.ToString())}():Promise<{nd2}[]>"))
                            {

                                var rmp3 = sm.GetTypeInfo(nd2);
                                //addAllNames(rmp3.Type, wr2);
                                addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: new List<ITypeSymbol>() { rmp3.Type });


                                var url = (class_.Parent as NamespaceDeclarationSyntax).Name + "." + class_.Identifier;
                                url = url.Replace(".", "__");

                                wr2.WriteLine($" var ar =await context.{nd2}Manager.getSubTable(\"v1/generic/{url}/\"+this.id+\"/{mem.Identifier.ToString()}\")!;");

                                wr2.WriteLine($" return  ar;");
                            }

                        }
                        continue;
                    }
                    var rmp2 = sm.GetTypeInfo(mem.Type);

                    //addOrUpdateManager(sm.GetDeclaredSymbol(class_), used:new List<ITypeSymbol>() { rmp2.Type });







                }

            }
            return res;

        }


        public static void handleType(TypeDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {


            addOrUpdateManager(sm.GetDeclaredSymbol(class_),  syntax: class_, sm: sm);
            using (var tt2 = tt.newClass($"export interface {GetName(class_)} {getHeader(class_, tt, sm)} "))
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                handleTypeMemeber(class_, tt2, sm, false);
            }

        }
        public static void handleEnums(EnumDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {

            var memType0 = sm.GetTypeInfo(class_);//sm.GetDeclaredSymbol(class_) ;
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, null, tt.getFileName(), isNonAbstractClass : false);
            using (var tt2 = tt.newClass($"export enum {class_.Identifier} "))
            {
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, block: tt2);
                foreach (var mem in class_.Members)
                {
                    //tt.WriteLine($"{mem.Identifier}");


                    var v = mem.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                    if (v != null)
                    {
                        var rmp2 = sm.GetConstantValue(v.Value);


                        //v.DescendantNodes().OfType<LiteralExpressionSyntax>();
                        tt2.WriteLine($"{mem.Identifier} = {rmp2},");

                    }
                    else
                    {
                        tt2.WriteLine($"{mem.Identifier} ,");
                    }

                }


                //.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
            }


        }
        public static void handleInterface(InterfaceDeclarationSyntax class_, IBlockDespose tt, SemanticModel sm)
        {
            var memType0 = sm.GetTypeInfo(class_);// sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), memType0, null, tt.getFileName(), isNonAbstractClass: false);

            handleType(class_, tt, sm);

        }
        public static IEnumerable<IPropertySymbol> getProps(ITypeSymbol h)
        {

            var z = h.GetMembers().OfType<IPropertySymbol>().Where(x => !x.GetAttributes().Any(y => y.AttributeClass.Name == "JsonIgnoreAttribute" || y.AttributeClass.Name == "JsonIgnore")
            && !(x.Type.Name.StartsWith("ICollection"))
            && !x.IsOverride
            );
            return z;
        }
        public static string GetName(TypeDeclarationSyntax class_)
        {
            var res = "";
            if (class_.TypeParameterList != null && class_.TypeParameterList.ChildNodes().ToList().Count() > 0)
                res = $"<{class_.TypeParameterList.ChildNodes().ToList().ConvertAll(x => x.ToString()).Aggregate((l, r) => $"{l},{r}")}>";
            return $"{class_.Identifier.ToString()}{res}";
        }
        public static string GetName2(TypeDeclarationSyntax class_)
        {
            var res = "";
            if (class_.TypeParameterList != null && class_.TypeParameterList.ChildNodes().ToList().Count() > 0)
                res = $"<{class_.TypeParameterList.ChildNodes().ToList().ConvertAll(x => x.ToString()).Aggregate((l, r) => $"{l},{r}")}>";
            return $"{class_.Identifier}{res}";
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
            addOrUpdateManager(sm.GetDeclaredSymbol(class_), classType, null, tt.getFileName(), syntax: class_, sm: sm);
         


            var superClassSymbol = class_.getBaseClass(sm);
            var hh = class_.GetBaseClasses(sm);
            var bases = class_.GetBaseClasses(sm);
            var baseId = bases.Where(x => x.GetMembers().Any(x => x.Name == "id")).FirstOrDefault();
            if (baseId != null)
            {
                var z = baseId.GetMembers().First(x => x.Name == "id");
                
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), keyType: (z as IPropertySymbol).Type);
              
            }
            if (superClassSymbol != null)
                addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: hh);
            

            //if(h!= null) 
            //    tt.WriteLine($"@Deserialize.inheritSerialization(() => {getTsName(h,sm)})");
            using (var tt2 = tt.newClass($"export class {GetName(class_)}  {getHeaderClass(class_, tt, sm)} "))
            {
                var res = handleTypeMemeber(class_, tt2, sm, true);

                var args = new List<Tuple<string, TsTypeInf>>();

                foreach (var superClassSymbol1 in hh)
                {

                    args.AddRange(getProps(superClassSymbol1).ToList().ConvertAll(x => new Tuple<string, TsTypeInf>(toCamel(x.Name), getTsName(x.Type, sm))));
                    addOrUpdateManager(sm.GetDeclaredSymbol(class_), used: getProps(superClassSymbol1).ToList().ConvertAll(x => x.Type));
                }
                args.AddRange(res.ConvertAll(x => new Tuple<string, TsTypeInf>(toCamel(x.Identifier.ToString()), getTsName(sm.GetTypeInfo(x.Type).Type, sm))));

                using (var hed = tt2.newConstructor(args))
                {

                    {
                        if (superClassSymbol != null)
                        {

                            hed.SuperCunstrocotrCall(new() { "args" });
                        }
                        foreach (var m in res)
                            hed.WriteLine($"this.{toCamel(m.Identifier.ToString())} = args.{toCamel(m.Identifier.ToString())};");
                    }
                }
                using (var hed = tt2.newFunction("toJson", new List<Tuple<string, string>>() { }, GetName(class_))) //TODO isclientCreatble or not 
                {


                    /*
                     * //@ts-ignore
                    this["$type"]="Models.ExamAction";
                    return this;
                    */
                    classType.ToString();
                    hed.WriteLine($" //@ts-ignore");
                    hed.WriteLine($" this[\"$type\"]=\"{sm.GetDeclaredSymbol(class_)}\"");
                    hed.WriteLine($" return this;");

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
            using (var tt2 = tt.newClass($"export type {GetName(class_)} = "))
            {
                //var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                var res = handleTypeMemeber(class_, tt2, sm, true);
                //managerMap[classType.Type].filds = res;
                //var z = classType.Type.GetMembers().OfType<IPropertySymbol>().Where(x => !x.GetAttributes().Any(y => y.AttributeClass.Name == "JsonIgnoreAttribute"));


                var args = new List<Tuple<string, TsTypeInf>>();
                //if (superClassSymbol != null)

                args.AddRange(res.ConvertAll(x => new Tuple<string, TsTypeInf>(x.Identifier.ToString(), getTsName(sm.GetTypeInfo(x.Type).Type, sm))));
                //args.AddRange(z.ToList().ConvertAll(x => new Tuple<string, string>(x.Name, getTsName(x.Type, sm))));
                if (false)
                    using (var hed = tt2.newConstructor(args))
                    {






                        {

                            foreach (var m in res)
                                hed.WriteLine($"this.{toCamel(m.Identifier.ToString())} = args.{m.Identifier.ToString()};");
                        }
                    }
            }




        }

        public static Dictionary<Project, Compilation> comp = new();
        public static Dictionary<SyntaxTree, SemanticModel> smC = new();
        public static Dictionary<SyntaxTree, CompilationUnitSyntax> rootC = new();
        static async Task Main(string[] args)
        {
            var rgx = new Regex(@"Models/Basics/*");
            Console.WriteLine(rgx.Match("Models/Basics/adf").Success);
            Console.WriteLine(rgx.Match("Models/Basics/adf/sdfs").Success);

            //var wr = new Writer() { writer = new FileWriter("D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\Models2.ts") };
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

                                //var wr = new Writer() { writer=new FileWriter("D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models2.ts") };
                                var fi = new FileInfo(prjfn);

                                var fn2 = tree.FilePath.Substring(fi.Directory.FullName.Length + 1, tree.FilePath.Length - fi.Directory.FullName.Length - 4);
                                //using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\{fn2}.ts"))
                                {

                                    var wr = new FileBlock(null, null, 0) { fn = fn2 };
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

                                    //fwriter.Flush();
                                }


        ;
                            }


                        }
                        for(int i=0;i<1;++i){
                            var t=managerMap.First(x => x.Key.ToString() == "Models.Response");
                            //SemanticModel model = smC[tree];
                            setIsHideFalseForDerivedClass(t.Key).Select(x=> setIsHideFalse(x.Key, new()));
                            t.Value.isClientCreatable = true;
                            t.Value.isPolimorphicBase = true;

                            t = managerMap.First(x => x.Key.ToString() == "Models.Question");
                            //SemanticModel model = smC[tree];
                            setIsHideFalseForDerivedClass(t.Key).Select(x => setIsHideFalse(x.Key, new()));
                            t.Value.isPolimorphicBase = true;


                            t = managerMap.First(x => x.Key.ToString() == "Models.ExamAction");
                            t.Value.isPolimorphicBase = true;
                            t.Value.isClientCreatable = true;
                            //SemanticModel model = smC[tree];
                            setIsHideFalseForDerivedClass(t.Key).Select(x => setIsHideFalse(x.Key, new()));
                        }

                        {
                            var project = solution.Projects.First(x => x.Name == "Data");
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            var ress = getContext(project, compilation);
                            var t = managerMap.Where(x => ress.Contains(x.Key));
                            foreach (var z in t)
                            {
                                z.Value.context = $"import {{ manager as context }} from \"Models/managers\"";
                                setIsHideFalse(z.Key, new());
                            }
                        }
                        {
                            var project = solution.Projects.First(x => x.Name == "old_Data");
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            var ress = getContext(project, compilation);
                            var t = managerMap.Where(x => ress.Contains(x.Key));
                            foreach (var z in t)
                            {
                                z.Value.context = $"import {{ oldManager as context }} from \"Models/oldManagers\"";
                                setIsHideFalse(z.Key, new());
                            }

                        }
                        {
                            var t = managerMap.Where(x => x.Key.ToString().StartsWith("ClientMsgs"));
                            foreach (var z in t)
                                setIsHideFalse(z.Key, new());
                        }
                        foreach (var ff in fns)
                            try
                            {
                                var cls = managerMap.Where(x => x.Value.fn == ff.fn && !x.Value.isHide).ToList();
                                HashSet<ITypeSymbol> usedTypes = new();
                                string context = "";
                                foreach (var cl in cls)
                                {
                                    if (cl.Value.context != null)
                                        context = cl.Value.context;
                                    foreach (var us in cl.Value.usedTypes)
                                        usedTypes.Add(us);
                                }
                                if (true)
                                {

                                    if (cls.Count > 0)
                                    {
                                        try
                                        {
                                            var path = $"D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\{ff.fn}.ts";
                                            if(File.Exists(path))
                                            File.WriteAllText(path, string.Empty);
                                            using (var fwriter = new FileWriter(path))
                                            {

                                                fwriter.WriteLine("import  { Guid,Forg,httpGettr,List,Dictionary,ForeignKey,ForeignKey2,Rial } from \"Models/base\";");
                                                //fwriter.WriteLine("import { IIdMapper , IdMapper} from \"Models/Basics/BaseModels/Basics/Basics\"");
                                                //fwriter.WriteLine("import * as Deserialize from \"dcerialize\"");

                                                fwriter.WriteLine(context);

                                                fwriter.WriteLine("\n\n");
                                                foreach (var tf in usedTypes)
                                                {
                                                    if (!managerMap.ContainsKey(tf))
                                                        continue;

                                                    if (managerMap[tf].fn != null && managerMap[tf].fn != ff.fn)
                                                    {
                                                        ImportWrite(tf, fwriter);
                                                    }

                                                }
                                                fwriter.WriteLine("\n\n");
                                                foreach (var tf in usedTypes)
                                                {
                                                    if (!managerMap.ContainsKey(tf))
                                                        continue;
                                                    if (managerMap[tf].isResource)
                                                        if (managerMap[tf].isOld)
                                                            fwriter.WriteLine($"import {{ {tf.Name}Manager }} from \"Models/oldManagers\"");
                                                        else
                                                            fwriter.WriteLine($"import {{ {tf.Name}Manager }} from \"Models/managers\"");

                                                }
                                                fwriter.WriteLine("\n\n");
                                                foreach (var cl in cls)
                                                    if (cl.Value.block != null)
                                                    {
                                                        fwriter.Write(cl.Value.block.ToString());
                                                        if (cl.Value.syntax != null && cl.Value.isNonAbstractClass && !cl.Value.isPolimorphicBase )
                                                        {
                                                            fwriter.WriteLine($"export var {GetName(cl.Value.syntax)}Creator = (args:any)=> new {GetName(cl.Value.syntax)}(args)");
                                                        }
                                                        else
                                                        if (cl.Value.syntax != null && cl.Value.isNonAbstractClass)
                                                        {
                                                            fwriter.WriteLine($"export var {GetName(cl.Value.syntax)}Creator = (args:any)=> {{const types={{");
                                                            fwriter.WriteLine($"\"{cl.Key}\":(args: any)=>new {GetName(cl.Value.syntax)}(args),");
                                                            
                                                            foreach (var e in managerMap)
                                                            {
                                                                if (e.Value.syntax == null)
                                                                    continue;
                                                                var bases = e.Value.syntax.GetBaseClasses(e.Value.sm);
                                                                
                                                                
                                                                if (bases.Contains(cl.Key))
                                                                {
                                                                    fwriter.WriteLine($"\"{e.Key}\":(args: any)=>new {e.Key.Name}(args),"); 
                                                                }
                                                            }
                                                            fwriter.WriteLine("};");
                                                            fwriter.WriteLine("if(!(\"$type\" in args)){\treturn new Response(args);}");
                                                            fwriter.WriteLine("return types[args[\"$type\"]](args);");
                                                            fwriter.WriteLine("}");
                                                        }

                                                    }

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
                        if (true)
                            using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\oldManagers.ts"))
                            {
                                fwriter.WriteLine("import { Guid, Forg,httpGettr } from \"./base\";");
                                fwriter.WriteLine("import { IEntityManager,EntityManager } from \"./baseManagers\";");
                                fwriter.WriteLine("\n\n");
                                var project = solution.Projects.First(x => x.Name == "old_Data");
                                var compilation = comp[project];// await project.GetCompilationAsync();
                                var ress = getContext(project, compilation);
                                var tt = managerMap.Where(x => ress.Contains(x.Key));

                                foreach (var t in tt)
                                {
                                    ImportWrite(t.Key, fwriter);
                                }


                                fwriter.WriteLine("\n\n");
                                fwriter.WriteLine("class Manager{");
                                foreach (var t in tt)
                                {
                                    var ss = t.Value.keyTypeName!=null ? getTsName(t.Value.keyTypeName).name:"number";
                                    fwriter.WriteLine($"   {t.Key.Name}Manager: EntityManager<{t.Key.Name},{ss}> =new EntityManager<{t.Key.Name},{ss}>(\"{t.Key}\",{t.Key.Name}Creator);");
                                }
                                fwriter.WriteLine("}");
                                fwriter.WriteLine("export var oldManager=new Manager()");
                            }
                        using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelper-react2\\src\\Models\\managers.ts"))
                        {
                            fwriter.WriteLine("import { Guid, Forg,httpGettr } from \"./base\";");
                            fwriter.WriteLine("import { IEntityManager,EntityManager } from \"./baseManagers\";");
                            fwriter.WriteLine("\n\n");
                            var project = solution.Projects.First(x => x.Name == "Data");
                            var compilation = comp[project];// await project.GetCompilationAsync();
                            var ress = getContext(project, compilation);
                            var tt = managerMap.Where(x => ress.Contains(x.Key));
                            foreach (var t in tt)
                                ImportWrite(t.Key, fwriter);


                            fwriter.WriteLine("\n\n");
                           
                            fwriter.WriteLine("\n\n");
                            fwriter.WriteLine("class Manager{");
                            foreach (var t in tt)
                            {
                                var ss = t.Value.keyTypeName != null ? getTsName(t.Value.keyTypeName).name : "number";
                                fwriter.WriteLine($"   {t.Key.Name}Manager: EntityManager<{t.Key.Name},{ss}> =new EntityManager<{t.Key.Name},{ss}>(\"{t.Key}\",{t.Key.Name}Creator);");
                            }
                            fwriter.WriteLine("}");
                            fwriter.WriteLine("export var manager=new Manager()");
                        }

                    }
                    catch (Exception e)
                    {

                    }

            }
            var pw = new ProjectWriter($"D:\\programing\\TestHelperTotal\\TestHelper-react2\\", "ts");
            for (int i = 0; i < 100; ++i)
            {
                var fls = new List<Regex>();
                //D:\programing\TestHelperTotal\TestHelper-react2\src\Models\Basics\BaseModels\Basics\Basics.ts
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
        public static void ImportWrite(ITypeSymbol tf, FileWriter fwriter)
        {
            var cs = $" , {tf.Name}Creator ";
            fwriter.WriteLine($"import {{ {tf.Name}  {(managerMap[tf].isNonAbstractClass ? cs : "")}}} from \"Models/{linuxPathStyle(managerMap[tf].fn)}\"");

            
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
                            var DBcontext = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(x => x.GetBaseClasses(model).Any(x => x.Name == "DbContext"));
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
        public static string toCamelClass(string name)
        {
            return Char.ToUpper(name[0]) + name.Substring(1);

        }
    }
}
