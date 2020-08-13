using System;

namespace DotHex
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var hex = new Hex(@"C:\some.hex");
            var lineNumber = hex.FindAbsAdrLineNumber("A001C000");
            Console.WriteLine(lineNumber);
        }
    }
}