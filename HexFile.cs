using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DailyTest
{
    public static class DotHex
    {
        public static byte StringToHex(string value)
        {
            return byte.Parse(value, NumberStyles.HexNumber);
        }

        public static string HexLine(string address, string recordType, string data)
        {
            var hexValues = new List<string>();

            // : - StartCode
            var sb = new StringBuilder(":");
            // xx - Data byte count
            var byteCount = (data.Length / 2).ToString("X");
            if (byteCount.Length % 2 != 0)
                byteCount = "0" + byteCount;
            sb.Append(byteCount);
            hexValues.AddRange(GetByteHexValues(byteCount));
            // xx - Address
            sb.Append(address);
            hexValues.AddRange(GetByteHexValues(address));
            // xx - RecordType
            sb.Append(recordType);
            hexValues.Add(recordType);
            // xx - Data
            sb.Append(data);
            hexValues.AddRange(GetByteHexValues(data));

            byte i = 0;
            foreach (var hexValue in hexValues)
                i += StringToHex(hexValue);

            var checkSum = HexTwosComplement(i.ToString("X"));

            if (checkSum == "F")
                checkSum = "FF";

            return sb + checkSum;
        }


        public static IEnumerable<string> GetByteHexValues(string data)
        {
            const int size = 2;
            for (var i = 0; i < data.Length; i += size)
            {
                yield return data.Substring(i, Math.Min(size, data.Length - i));
            }
        }


        public static string HexTwosComplement(string hexValue)
        {
            var onesComplement = FlipZeroAndOne(HexToBinary(hexValue));

            var twosComplement = Convert.ToInt32(onesComplement, 2) + 1;

            return HexLsbValue(twosComplement);
        }

        public static string HexLsbValue(int value)
        {
            // var hexString = value.ToString("X");
            // return hexString.Substring(Math.Max(0, hexString.Length - 2));
            var bytes = BitConverter.GetBytes(value);
            var bytesArray = BitConverter.ToString(bytes).Split('-');
            return bytesArray[0];
        }

        public static string HexToBinary(string hexValue)
        {
            var charArray = hexValue.ToCharArray();
            var sb = new StringBuilder();

            foreach (var character in charArray)
            {
                sb.Append(Convert.ToString(Convert.ToInt32(character.ToString(), 16), 2).PadLeft(4, '0'));
            }

            return sb.ToString();
        }

        public static string FlipZeroAndOne(string binaryString)
        {
            // 0 -> _
            var noZeroTemp = binaryString.Replace('0', '_');
            // 1 -> 0
            var noOneTemp = noZeroTemp.Replace('1', '0');
            // _ -> 1
            return noOneTemp.Replace('_', '1');
        }
    }
}