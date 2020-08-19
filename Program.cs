using System;

namespace DotHex
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // 35
            Console.WriteLine(Hex.GenerateHexLine("0000", RecordType.Data, "21A8"));
            // 59
            Console.WriteLine(Hex.GenerateHexLine("3FC0", RecordType.Data, "20202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202020202000F85000000000"));
            Console.ReadLine();
        }
    }
}