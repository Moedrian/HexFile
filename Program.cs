using System;

namespace DotHex
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var hex = new Hex(@"C:\Dev\Storage\hexes\V1_STG_VST_MRSM_SBW_LFT_3\SCCM_APPL_FBL_ICT_HSM_UCB_ShePlus.hex");
            var lineNumber = hex.FindAbsAdrLineNumber("A001C000");
            Console.WriteLine(lineNumber);
        }
    }
}