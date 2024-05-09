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
        public class TypeDes
        {
            public string name;
            public string fn;

            public bool isResource { get; internal set; }
        }
        public static Dictionary<ITypeSymbol, TypeDes> managerMap = new() {  };
        public class TsTypeInf
        {

        }
        private static string getTsName(string type)
        {
            var x = type;
            if (x.EndsWith('?'))
            {
                x = x.Substring(0, x.Length - 1);
                return getTsName(x)+" | undefined";
            }
            string res;
            if (tsMap.TryGetValue(type, out res))
                return res;
            
            return type;
        }
        private static string getTsName(TypeInfo type,SemanticModel sm) {


            
            return getTsName(type.Type,sm);
        }
        private static string getTsName(ITypeSymbol type,SemanticModel sm)
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
            return getTsName(tt,sm);
        }
        
        private static List<ITypeSymbol> GetAllNames(ITypeSymbol type)
        {
            var res = new List<ITypeSymbol>();
            //var tt = sm.GetTypeInfo(type0);
            //var type = tt.Type;
            res.Add(type);

            if (type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {
                foreach(var x in s2.TypeArguments)
                    res.AddRange(GetAllNames(x));

                return res;
            }
            return res;
        }
        private static void addAllNames(ITypeSymbol type, IBlockDespose wr)
        {
            foreach(var t in GetAllNames(type))
            {
                wr.addUsedType(t);
            }
        }


        private static TypeDes addOrUpdateManager(ITypeSymbol type, string keyType,string fn)
        {
            TypeDes res;
            if (!managerMap.TryGetValue(type, out res))
                managerMap[type] = res = new TypeDes();
            if (fn != null)
                res.fn = fn;
            if (keyType!=null)
                res.name = keyType;
            return res;
        }
        public static IEnumerable<TypeInfo> getBases(TypeDeclarationSyntax class_, Compilation compilation)
        {

            return class_.BaseList?.Types.Select(x =>
            { //x.ToString()
                return compilation.GetSemanticModel(class_.SyntaxTree).GetTypeInfo(x.Type);
                //return z.Type;
            });
        }
        
        public static ITypeSymbol getBaseClass(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {

            if (class_.BaseList != null)
            {
                var first = true;
                foreach (var base_ in class_.BaseList.Types)
                {
                    
                    var rmp2 = sm.GetTypeInfo(base_.Type);

                    if (rmp2.Type.TypeKind == TypeKind.Class)
                        return rmp2.Type;
                    else
                        return null;
                }
            }
            return null;

            
        }
        public static List<ITypeSymbol> getInterfaces(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            var res = new List<ITypeSymbol>();
            if (class_.BaseList != null)
            {
                
                foreach (var base_ in class_.BaseList.Types)
                {
                    
                    var rmp2 = sm.GetTypeInfo(base_.Type);
                    addAllNames(rmp2.Type,tt2);
                    if (rmp2.Type.TypeKind == TypeKind.Interface)
                        res.Add(rmp2.Type);
                        
                }
            }
            return res;


        }
        public static string getHeader(TypeDeclarationSyntax class_, IBlockDespose tt2, Compilation compilation)
        {
            var sm = compilation.GetSemanticModel(class_.SyntaxTree);
            var interfaces = getInterfaces(class_, tt2, sm);
            if (interfaces.Count() > 0)
                return $" extends {interfaces.ConvertAll(x => getTsName(x, sm)).Aggregate((l, r) => $"{l},{r}")}";
            return "";
            
        }
        public static string getHeaderClass(TypeDeclarationSyntax class_, IBlockDespose tt2, SemanticModel sm)
        {
            string res = "";
            

            var baseClass = getBaseClass(class_,tt2, sm);

            var interfaces = getInterfaces(class_,tt2, sm);

            if (baseClass != null) 
                res += $" extends {getTsName(baseClass, sm)}";
            if (interfaces.Count() >0)
                res += $" implements {interfaces.ConvertAll(x=> getTsName(x,sm)).Aggregate((l,r)=>$"{l},{r}")}";
            return res;
        }
        public static PropertyDeclarationSyntax getPropWithName(TypeDeclarationSyntax class_, string name)
        {
            return class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Identifier.ToString() == name).First();
        }
        public class St
        {
            //public string fieldName;
            public string enityFildNameName;
            public string keyType;
            public bool nullable = false;
            public TypeSyntax entityType;
        }
        public static Dictionary<string, St> findForgienKeyMen(TypeDeclarationSyntax class_, SemanticModel sm)
        {

            var res = new Dictionary<string, St>();
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                foreach (var mem in mems)
                {

                    var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();

                    if (anot.Where(x => x.Name.ToString() == "ForeignKey").Any())
                    {
                        var z = anot.Where(x => x.Name.ToString() == "ForeignKey").First();
                        var tz = z.ArgumentList.Arguments.First();

                        {

                            var rmp2 = sm.GetConstantValue(tz.Expression);

                            var idMemeber = getPropWithName(class_, rmp2.ToString());
                            {
                                var tt = sm.GetTypeInfo(idMemeber.Type);
                                var type = tt.Type;
                                ITypeSymbol s = type as INamedTypeSymbol;
                                if (type.OriginalDefinition.Name == "Nullable")
                                {


                                    
                                    s = (s as INamedTypeSymbol).TypeArguments.FirstOrDefault();
                                    
                                }
                                
                                Console.WriteLine("");
                                res[idMemeber.Identifier.ToString()] = new St()
                                {
                                    enityFildNameName= mem.Identifier.ToString(),
                                    entityType = mem.Type,
                                    keyType = getTsName(s, sm),
                                    nullable = type.OriginalDefinition.Name == "Nullable"
                                };
                                mem.Type.GetNamespace();
                                var resName = sm.GetTypeInfo(getPropWithName(class_, rmp2.ToString()).Type).Type.ToString();// getPropWithName(class_, rmp2.ToString()).Type.GetFullName();
                                var memType = sm.GetTypeInfo(mem.Type).Type.ToString(); //sm.GetTypeInfo(mem.Type).GetFullName();
                                var memType0 = sm.GetTypeInfo(mem.Type).Type;
                                if (memType=="int" || memType=="string" || memType == "System.Guid"
                                    || memType == "int?" || memType == "string?" || memType == "System.Guid?"
                                    || memType0.TypeKind == TypeKind.Struct
                                    )
                                {
                                    var tmp = memType;
                                    memType = resName;
                                    resName = tmp;
                                }
                                
                                
                                addOrUpdateManager(memType0, resName, null).isResource = true;
                            }


                        }

                    }
                }
            }
            return res;


        }
        public class A
        {
            public SyntaxToken name { get; internal set; }
            public TypeInfo type { get; internal set; }
        }
        public static List<A> handleTypeMemeber(TypeDeclarationSyntax class_, IBlockDespose tt2, Compilation compilation,bool isClass)
        {
            List<A> res = new List<A>();
            
            var sm = compilation.GetSemanticModel(class_.SyntaxTree);
            var frs=findForgienKeyMen(class_, sm);
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
                            using (var wr2 = tt2.newBlock($"async get{mem.Identifier.ToString()}():Promise<{nd2}[]>")) {

                                var rmp3 = sm.GetTypeInfo(nd2);
                                addAllNames(rmp3.Type,wr2);
                                wr2.WriteLine("//this code must be handle catch");
                                var url=(class_.Parent as NamespaceDeclarationSyntax).Name+"."+ class_.Identifier;
                                url=url.Replace(".", "__");
                                
                                wr2.WriteLine($" var ar =await httpGettr.Get<{nd2}>(\"v1/generico/{url}/\"+this.id+\"/{mem.Identifier.ToString()}\")!;");
                                wr2.WriteLine($" // TODO {nd2}Manager.update(ar);");
                                wr2.WriteLine($" return  ar;");
                            }

                        }
                        continue;
                    }
                    var rmp2 = sm.GetTypeInfo(mem.Type);

                    if (anot.Where(x => x.Name.ToString() == "ForeignKey").Any() )
                    {

                        
                        var z = anot.Where(x => x.Name.ToString() == "ForeignKey").First();
                        var tz = z.ArgumentList.Arguments.First();
                        tt2.WriteLine($"// {tz}");
                        //tz.ChildNodes().ToList();

                        //if (tz.ChildNodes().FirstOrDefault() is InvocationExpressionSyntax ine)
                        {
                            var rmp3 = compilation.GetSemanticModel(class_.SyntaxTree).GetConstantValue(tz.Expression);
                            //addOrUpdateManager(mem.Type.ToString(), getPropWithName(class_, rmp3.ToString()).Type.ToString(), null).isResource = true;
                            addAllNames(rmp2.Type,tt2);
                        }
                        tt2.WriteLine($"// {tz}");
                        continue;



                    }
                    if (anot.Where(x => x.Name.ToString() == "JsonIgnore").Any())
                        continue;

                    //Console.WriteLine(mem.Type);
                    //Console.WriteLine(mem.Identifier.ToString());
                    addAllNames(rmp2.Type,tt2);
                    
                    var a = new A()
                    {
                        name = mem.Identifier,
                        type = rmp2
                    };
                    res.Add(a);

                    St typeo;
                    if (frs.TryGetValue(mem.Identifier.ToString(), out typeo))
                    {
                        addAllNames(rmp2.Type,tt2);
                        //tt2.WriteLine($"@Deserialize.autoserializeAs(() => {typeo.keyType})");
                        if(typeo.nullable)
                            tt2.WriteLine($"{toCamel(mem.Identifier.ToString())}? : Forg<{typeo.entityType},{typeo.keyType}>;");
                        else
                            tt2.WriteLine($"{toCamel(mem.Identifier.ToString())} : Forg<{typeo.entityType},{typeo.keyType}>;");
                        continue;
                    }
                    {
                        
                        Console.WriteLine(rmp2.ToString());

                        //if(mem.Type.Kind()==
                        //tt2.WriteLine($"{mem.Identifier.ToString()} : {getTsName(mem.Type)};");

                        if (isClass)
                            if (a.type.Type.OriginalDefinition.Name == "Nullable")
                            {
                                var s = a.type.Type as INamedTypeSymbol;
                                var ts = getTsName(s.TypeArguments.FirstOrDefault(), sm);
                                //tt2.WriteLine($"@Deserialize.autoserializeAs(() => {ts})");
                            }
                            else
                            {
                                /*if (a.type.Type.TypeKind == TypeKind.Enum)
                                    tt2.WriteLine($"@Deserialize.autoserializeAs(() => Number)");
                                else if(a.type.Type.Name=="Guid")
                                    tt2.WriteLine($"@Deserialize.autoserializeAs(() => String)");
                                else
                                    tt2.WriteLine($"@Deserialize.autoserializeAs(() => {getTsName(a.type, sm)})");*/
                            }

                        tt2.WriteLine($"{toCamel(a.name.ToString())} : {getTsName(a.type, sm)};");
                    }
                    
                    if(false)
                    {
                        addAllNames(rmp2.Type, tt2);
                        var rmp3 = sm.GetTypeInfo(mem.Type);
                        Console.WriteLine(rmp3.ToString());
                        //if(mem.Type.Kind()==
                        tt2.WriteLine($"{mem.Identifier.ToString()} : {getTsName(mem.Type,sm)};");
                    }
                    
                }
                foreach(var z in frs)
                {
                    //addAllNames(z.Value.entityType, tt2);
                    using (var wr3 = tt2.newBlock($"async get{z.Value.enityFildNameName}():Promise<{z.Value.entityType}>"))
                    {
                        wr3.WriteLine("//this code must handle async and sync 2");
                        if(z.Value.nullable)
                            wr3.WriteLine($" return await {z.Value.entityType}Manager.get(this.{toCamel(z.Key)}!);");
                        else
                            wr3.WriteLine($" return await {z.Value.entityType}Manager.get(this.{toCamel(z.Key)});");
                        //addOrUpdateManager(mem.Type.ToString(), getPropWithName(class_, rmp3.ToString()).Type.ToString(), null).isResource = true;
                    }
                }
            }
            return res;

        }

        
        public static void handleType(TypeDeclarationSyntax class_, IBlockDespose tt, Compilation compilation)
        {

            //var rmp2 = compilation.GetSemanticModel(class_.SyntaxTree).GetTypeInfo(class_);
            //var ttdf=rmp2.Type.BaseType;
            using (var tt2 = tt.newBlock($"export interface {GetName(class_)} {getHeader(class_,tt,compilation)} "))
            {
                var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                handleTypeMemeber(class_,tt2,compilation,false);
            }

        }
        public static void handleEnums(EnumDeclarationSyntax class_, IBlockDespose tt, Compilation compilation)
        {
            var sm = compilation.GetSemanticModel(class_.SyntaxTree);
            var memType0 = sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(memType0, null, tt.getFileName());
            using (var tt2 = tt.newBlock($"export enum {class_.Identifier} "))
            {
                foreach(var mem in class_.Members)
                {
                    //tt.WriteLine($"{mem.Identifier}");

                    
                    var v=mem.DescendantNodes().OfType<EqualsValueClauseSyntax>().FirstOrDefault();
                    if (v != null)
                    {
                        var rmp2 = compilation.GetSemanticModel(class_.SyntaxTree).GetConstantValue(v.Value);
                        

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
        public static void handleInterface(InterfaceDeclarationSyntax class_, IBlockDespose tt, Compilation compilation)
        {
            var sm = compilation.GetSemanticModel(class_.SyntaxTree);
            var memType0 = sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(memType0, null, tt.getFileName());
            handleType(class_, tt, compilation);
        
        }
        public static IEnumerable<IPropertySymbol> getProps(ITypeSymbol h)
        {
            
            var z = h.GetMembers().OfType<IPropertySymbol>().Where(x=> !x.GetAttributes().Any(y=> y.AttributeClass.Name== "JsonIgnoreAttribute"));
            return z;
        }
        public static string GetName(TypeDeclarationSyntax class_)
        {
            var res = "";
            if (class_.TypeParameterList != null && class_.TypeParameterList.ChildNodes().ToList().Count()>0)
                res=$"<{class_.TypeParameterList.ChildNodes().ToList().ConvertAll(x => x.ToString()).Aggregate((l, r) => $"{l},{r}")}>";
            return $"{class_.Identifier.ToString()}{res}";
        }
        public static void handleClass(ClassDeclarationSyntax class_, IBlockDespose tt, Compilation compilation)
        {
            var sm = compilation.GetSemanticModel(class_.SyntaxTree);
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

            var memType0 = sm.GetDeclaredSymbol(class_);
            addOrUpdateManager(memType0, null, tt.getFileName());


            
            var h = getBaseClass(class_,tt, sm);
            if(h!=null)
                addAllNames(h, tt);
            //if(h!= null) 
            //    tt.WriteLine($"@Deserialize.inheritSerialization(() => {getTsName(h,sm)})");
            using (var tt2 = tt.newBlock($"export class {GetName(class_)}  {getHeaderClass(class_,tt,sm)} "))
                {
                    var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                    var res = handleTypeMemeber(class_, tt2, compilation,true);


                var hed=tt2.newConstructor();


                //string hed = "constructor(";
                List<string> hh = new();
                if (h != null)
                {
                    hed.AddRange(getProps(h).ToList().ConvertAll(x => new Tuple<string, string>(x.Name,getTsName(x.Type, sm))));
                    //hh.AddRange(getProps(h).ToList().ConvertAll(x => $" {x.Name}:{getTsName(x.Type,sm)}"));
                }
                hed.AddRange(res.ConvertAll(x => new Tuple<string, string>(x.name.ToString(), getTsName(x.type, sm))));
                //hh.AddRange(res.ConvertAll(r => $"{r.name}:{getTsName(r.type,sm)}"));

                //if(hh.Count()>0)
                //    hed+=hh.Aggregate((l, r) => $"{l},{r}");
                
                //hed += ")";
                using (var bl = tt2.newBlock(hed.ToString()))
                {
                    if (h != null)
                    {
                        tt2.SuperCunstrocotrCall(getProps(h)?.ToList().ConvertAll(x => x.Name));
                    }
                    foreach (var m in res)
                        tt2.WriteLine($"this.{toCamel(m.name.ToString())} = {m.name}");
                }
             }

            


        }
        static async Task Main(string[] args)
        {
            var rgx=new Regex(@"Models/Basics/*");
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
                for (int ji = 0; ji < 100; ++ji)
                    try{
                        managerMap.Clear();
                        fns.Clear();

                        foreach (var project in solution.Projects)
                        {
                            if (project.Name != "old_Models" && project.Name != "ViewGeneratorBase" && project.Name!="Models" && project.Name != "BaseModels")
                                continue;
                            var compilation = await project.GetCompilationAsync();
                            foreach (var tree in compilation.SyntaxTrees)
                            {
                                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                                SemanticModel model = compilation.GetSemanticModel(tree);
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

                                    var wr = new FileBlock() { fn = fn2 };
                                    fns.Add(wr);

                                    var nameSpaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

                                    foreach (var namespace_ in nameSpaces)
                                    {
                                        using (var tt = wr.newNameSpace(namespace_.Name.ToString()))
                                        {
                                            var enums = namespace_.DescendantNodes().OfType<EnumDeclarationSyntax>();
                                            foreach (var interface_ in enums)
                                                try
                                                {
                                                    handleEnums(interface_,
                                                                wr,
                                                                compilation);
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
                                                                compilation);
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
                                                                compilation);
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

                        var pr = managerMap.Values.Where(x => x.isResource).ToList();
                        for (int ti = 0; ti < pr.Count(); ++ti)
                        try{
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
                                            fwriter.WriteLine("import { Guid,Forg,httpGettr } from \"Models/base\";");
                                            //fwriter.WriteLine("import * as Deserialize from \"dcerialize\"");
                                        
                                            //fwriter.WriteLine("import * as Deserialize from 'dcerialize'");

                                            fwriter.WriteLine("\n\n");
                                            foreach (var tf in ff.usedTypes)
                                            {
                                                if (!managerMap.ContainsKey(tf))
                                                    continue;
                                                if (managerMap[tf].fn != ff.fn)
                                                {
                                                    fwriter.WriteLine($"import {{ {tf.Name} }} from \"Models/{linuxPathStyle(managerMap[tf].fn)}\"");
                                                    if (!pr.Contains(managerMap[tf]))
                                                        pr.Add(managerMap[tf    ]);
                                                }

                                            }
                                            fwriter.WriteLine("\n\n");
                                            foreach (var tf in ff.usedTypes)
                                            {
                                                if (!managerMap.ContainsKey(tf))
                                                    continue;
                                                if (managerMap[tf].isResource)
                                                    fwriter.WriteLine($"import {{ {tf}Manager }} from \"Models/managers\"");

                                            }
                                            fwriter.WriteLine("\n\n");
                                            foreach (var l in ff.lines)
                                                fwriter.WriteLine(l);

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
                    }
                    catch(Exception e)
                    {

                    }
            }
            var pw = new ProjectWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\","ts");
            for (int i=0; i<100; ++i)
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
                                fwriter.WriteLine(l);

                            //Deserialize.RuntimeTypingSetTypeString(EntityList<CoachType>, "Models.EntityList<Coach>");

                            fwriter.Flush();
                        }
                    }
            }
            using (var fwriter = new FileWriter($"D:\\programing\\TestHelperTotal\\TestHelperWebsite\\src\\Models\\managers.ts"))
            {
                fwriter.WriteLine("import { Guid, Forg,httpGettr } from \"./base\";");
                fwriter.WriteLine("import { IEntityManager,EntityManager } from \"./baseManagers\";");
                fwriter.WriteLine("\n\n");
                foreach (var t in managerMap)
                    if (t.Value.isResource )
                    {
                        fwriter.WriteLine($"import {{ {t.Key} }} from \"Models/{linuxPathStyle(t.Value.fn)}\"");
                }


                fwriter.WriteLine("\n\n");
                foreach (var t in managerMap)
                    if (t.Value.isResource)
                    {
                        fwriter.WriteLine($"export var {t.Key}Manager=new EntityManager<{t.Key},Number>(\"{t.Key}\");");
                    }
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

        static async Task Main2(string[] args)
        {

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
            using (var workspace = MSBuildWorkspace.Create())
            {


                //var solution = await workspace.OpenSolutionAsync("D:\\programing\\trade\\StockSharp\\StockSharp.sln");
                var solution = await workspace.OpenSolutionAsync("D:\\programing\\TestHelperTotal\\testhelper\\EnglishToefl.sln");
                //var solution = await workspace.OpenSolutionAsync("D:\\programing\\cs\\TestForAnalys\\ConsoleApp1\\ConsoleApp1.sln");// D:\\programing\\trade\\StockSharp\\StockSharp.sln");

                foreach (var project in solution.Projects)
                {
                    if (project.Name != "Models")
                        continue;
                    var compilation = await project.GetCompilationAsync();
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
                        SemanticModel model = compilation.GetSemanticModel(tree);
                        // </Snippet2>

                        // <Snippet6>
                        /*string s = root.FilePath;
                        if (!s.Contains("Models\\Models"))
                            continue;/**/

                        var wr = new FileBlock();
                        var nameSpaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

                        foreach (var namespace_ in nameSpaces)
                        {
                            var tt = wr.newNameSpace(namespace_.Name.ToString());
                            var classes = namespace_.DescendantNodes().OfType<ClassDeclarationSyntax>();
                            try
                            {
                                foreach (var class_ in classes)
                                {
                                    using (wr.newBlock($"class {class_.Identifier}  bases "))
                                    {
                                        var mems = class_.Members.OfType<PropertyDeclarationSyntax>().Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PublicKeyword))).ToList();
                                        //var bases=class_.BaseList;
                                        //var mems = class_.DescendantNodes().OfType<BaseListSyntax>().ToList();
                                        foreach (var mem in mems)
                                        {

                                            var anot = mem.DescendantNodes().OfType<AttributeSyntax>().ToList();
                                            if (mem.Type is GenericNameSyntax genericNameSyntax)
                                            {
                                                var nd = genericNameSyntax.ChildNodes();
                                                if (genericNameSyntax.Identifier.ToString() == "ICollection")
                                                {
                                                    var nd2 = genericNameSyntax.TypeArgumentList.Arguments.ToList()[0];
                                                    using (wr.newBlock($"get{mem.Identifier.ToString()}():{nd2}"))
                                                    {
                                                        wr.WriteLine("//this code must be handle catch");
                                                        
                                                    }

                                                }
                                                continue;
                                            }
                                            if (anot.Where(x => x.Name.ToString() == "JsonIgnore").Any())
                                                continue;
                                            //Console.WriteLine(mem.Type);
                                            //Console.WriteLine(mem.Identifier.ToString());
                                            wr.WriteLine($"{toCamel(mem.Identifier.ToString())} : {getTsName(mem.Type,null)} ;");
                                        }
                                    }

                                }
                            }
                            catch
                            {

                            }
                        }

                        var collector = new GenerateTs()
                        {
                            compilation = compilation,
                            model = model,
                            //filePath=root.FilePath
                        };

                        //collector.Visit(root);
                        foreach (var directive in collector.Usings)
                        {
                            map.Add(directive.ToString());

                        }
                    }


                }


            }

            foreach (var directive in map)
            {

                WriteLine($" public static const string {directive}=nameof({directive});");
            }



            // <Snippet2>

            // </Snippet6>
        }
    }
}
