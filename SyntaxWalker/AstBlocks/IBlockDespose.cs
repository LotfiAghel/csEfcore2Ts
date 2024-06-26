using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxWalker.AstBlocks;
using SyntaxWalker.AstBlocks.ts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxWalker
{
    public interface ILangSuport
    {
        public static ILangSuport Instance;

        IClassBlock newClassBlock(TypeDeclarationSyntax class_, SemanticModel sm, IBlockDespose fileBlock, int v);
        TsTypeInf getTsName(string type);
        public static TsTypeInf getTsName(TypeInfo type, SemanticModel sm)
        {



            return ILangSuport.getTsName(type.Type, sm);
        }

        public static TsTypeInf getTsName(TypeSyntax type, SemanticModel sm)
        {

            var tt = sm.GetTypeInfo(type);
            return getTsName(tt, sm);
        }
        public static TsTypeInf getTsName(ITypeSymbol type, SemanticModel sm)
        {
            //return "TODO";
            //var tt=sm.GetTypeInfo(type);
            if (type.OriginalDefinition.Name == "Nullable")
            {

                Console.WriteLine("");
                var s = type as INamedTypeSymbol;
                var z = getTsName(s.TypeArguments.FirstOrDefault(), sm);
                z.nullable = true;
                return z;
                Console.WriteLine("");
            }
            if (type is INamedTypeSymbol s2 && s2.TypeArguments != null && s2.TypeArguments.Count() > 0)
            {

                Console.WriteLine("");
                var res = ILangSuport.Instance.getTsName(type.Name);
                //res.type0 = type;
                res.name += "<";

                res.name += s2.TypeArguments.ToList().ConvertAll(x => getTsName(x, sm).name).Aggregate((l, r) => $"{l},{r}");
                res.name += ">";
                return res;
            }
            return ILangSuport.Instance.getTsName(type.Name);

        }

        IFileBlock newFileBlock(string fn2);
        string getPath(string fn);
        void ImportWrite(ITypeSymbol tf, FileWriter fwriter, Dictionary<ITypeSymbol, TypeDes> managerMap);
        void ImportBasic(FileWriter fwriter);
    }
    public interface IBlockDespose: IDisposable
    {
        public int tab { get; set; }
        List<IBlockDespose> lines { get; set; }
        HashSet<ITypeSymbol> usedTypes { get; set; }
        BlockDespose newBlock(string text);
        IClassBlock newClass(TypeDeclarationSyntax class_, SemanticModel sm);
        IEnumBlock newEnum(EnumDeclarationSyntax class_, SemanticModel sm);
        IClassBlock newStruct(TypeDeclarationSyntax class_, SemanticModel sm);
        IBlockDespose newFunction(string name, List<IPropertySymbol> args,string returnType,bool isAsync=false);
        void WriteLine(string text);
        string getFileName();
        void addUsedType(ITypeSymbol type);
    }
}
