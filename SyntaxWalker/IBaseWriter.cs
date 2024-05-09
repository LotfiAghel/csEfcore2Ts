using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace SyntaxWalker
{
    public interface IBaseWriter
    {
        void Write(string text);
        void WriteLine(string text);
    }

    public class ConsoleWriter : IBaseWriter
    {
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
        public void Write(string text)
        {
            Console.Write(text);
        }
    }

}
