using System;


namespace DotHex
{
    internal class Program
    {
        private const string Address = "--address";
        private const string Data = "--data";
        private const string RecordType = "--record-type";

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Arguments cannot leave blank");
                return;
            }

            string address = string.Empty;
            string recordType = string.Empty;
            string data = string.Empty;
            foreach (var arg in args)
            {
                var input = arg.Split('=');
                switch (input[0])
                {
                    case Address:
                        address = input[1];
                        break;
                    case Data:
                        data = input[1];
                        break;
                    case RecordType:
                        recordType = input[1];
                        break;
                    default:
                        Console.WriteLine("Argument name error, please check them");
                        return;
                }
            }

            // default record type DATA
            if (recordType.Length == 0)
                recordType = "00";

            Console.WriteLine(Hex.GenerateHexLine(address, Hex.ReflectRecordType(recordType), data));
        }
    }
}