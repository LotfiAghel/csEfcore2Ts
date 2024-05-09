using System;
using System.IO;

namespace SyntaxWalker
{
    public class ProjectWriter
    {
        public string path;
        public string passwand;

        public ProjectWriter(string v,string pas)
        {
            path = v;
            passwand = pas;
        }

        public FileWriter getFile(string path2)
        {
            return new FileWriter($"{path}{path2}.{passwand}");
        }
    }
    public class FileWriter : IBaseWriter,IDisposable
    {
        FileStream fs;
        StreamWriter stream;
        public FileWriter(string path)
        {
            FileInfo fi = new FileInfo(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            fs = fi.Open(FileMode.OpenOrCreate, FileAccess.Write);//, FileShare.Write);
            
            stream = new StreamWriter(fs);

            
        }
        public void WriteLine(string text)
        {
            stream.WriteLine(text);
            Console.WriteLine(text);
        }
        public void Write(string text)
        {
            stream.Write(text);
            Console.Write(text);
            
        }
        public void Flush()
        {
            stream.Flush();
        }

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
            fs.Dispose();
            Console.WriteLine("file closed");
        }

        public void WriteHeader()
        {
            WriteLine("import { Guid,Forg,httpGettr } from \"Models/base\";\n\n");
        }
    }

}
