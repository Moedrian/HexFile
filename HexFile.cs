using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace DotHex
{
    public class Hex
    {
        private readonly HexFileType _hexFileType;
        private readonly string _hexFilename;

        private const string StartCode = ":";
        private const string SpecialRecAdr = "0000";
        private const int LineStartOffset = 9;

        public Hex(string hexFilename, HexFileType hexFileType = HexFileType.Hex386)
        {
            _hexFilename = hexFilename;
            _hexFileType = hexFileType;
        }


        public int FindAbsAdrLineNumber(string hexAdr)
        {
            var targetLine = 1;

            if (_hexFileType == HexFileType.Hex386)
            {
                switch (hexAdr.Length)
                {
                    case 4:
                    {
                        var lineCtr = 1;
                        foreach (var line in File.ReadLines(_hexFilename))
                        {
                            var record = new RecordLine(line);
                            if (record.Address == hexAdr)
                                return lineCtr;
                            lineCtr++;
                        }

                        break;
                    }
                    case 8:
                    {
                        var extendedAddress = hexAdr.Substring(0, 4);
                        var lineAdr = hexAdr.Substring(4, 4);
                        var extAdrLine = GenerateHexLine(SpecialRecAdr, RecordType.ExtendedLinearAddress, extendedAddress);

                        var extAdrLineNumber = 1;

                        var lineCtr = 1;
                        var foundFlag = false;
                        foreach (var line in File.ReadLines(_hexFilename))
                        {
                            // Find the line describing line extension
                            if (line == extAdrLine)
                            {
                                extAdrLineNumber = lineCtr;
                                foundFlag = true;
                            }

                            // Continue to find the address
                            if (foundFlag)
                            {
                                var record = new RecordLine(line);
                                if (record.Address == lineAdr && record.RecordType == RecordType.Data)
                                {
                                    targetLine = extAdrLineNumber;
                                    break;
                                }

                                extAdrLineNumber++;
                            }

                            lineCtr++;
                        }

                        break;
                    }
                }
            }

            return targetLine;
        }


        private class RecordLine
        {
            public readonly int DataLength;
            public readonly string Address;
            public readonly string RecordType;
            public string Data;

            public RecordLine(string dataLine)
            {
                Address = dataLine.Substring(3, 4);
                RecordType = dataLine.Substring(7, 2);
                Data = dataLine.Substring(LineStartOffset, DataLength * 2);
                DataLength = int.Parse(dataLine.Substring(1, 2), NumberStyles.HexNumber);
            }
        }


        public static string GenerateHexLine(string address, string recordType, string data)
        {
            var hexValueString = new StringBuilder();

            // Data byte count
            var byteCount = (data.Length / 2).ToString("X");
            if (byteCount.Length % 2 != 0)
                byteCount = "0" + byteCount;
            hexValueString.Append(byteCount);

            // Address
            hexValueString.Append(address);

            // RecordType
            hexValueString.Append(recordType);

            // Data
            hexValueString.Append(data);

            // Generate Checksum
            var checkSum = GetChecksum(hexValueString);

            return StartCode + hexValueString + checkSum;
        }


        private static IEnumerable<string> GetByteHexValues(string data)
        {
            const int size = 2;
            for (var i = 0; i < data.Length; i += size)
            {
                yield return data.Substring(i, Math.Min(size, data.Length - i));
            }
        }


        private static string GetChecksum(StringBuilder hexValueString)
        {
            var hexByteValues = new List<string>(GetByteHexValues(hexValueString.ToString()));

            var i = 0;
            foreach (var byteValue in hexByteValues)
                i += int.Parse(byteValue, NumberStyles.HexNumber);

            // Hex string to Binary string
            var charArray = i.ToString("X").ToCharArray();
            var convertedBinaryString = new StringBuilder();

            foreach (var character in charArray)
            {
                var digit = Convert.ToString(Convert.ToInt16(character.ToString(), 16), 2).PadLeft(4, '0');
                convertedBinaryString.Append(digit);
            }

            // Flip Zero and One
            // 0 -> _
            var flippedZero = convertedBinaryString.ToString().Replace('0', '_');
            // 1 -> 0
            var flippedOne = flippedZero.Replace('1', '0');
            // _ -> 1
            var onesComplement = flippedOne.Replace('_', '1');

            // Get Two's Complement
            var twosComplement = Convert.ToInt32(onesComplement, 2) + 1;

            var bytes = BitConverter.GetBytes(twosComplement);
            var bytesArray = BitConverter.ToString(bytes).Split('-');
            // Little Endian
            var checkSum = bytesArray[0];

            if (checkSum == "F")
                checkSum = "FF";

            return checkSum;
        }
    }


    public enum HexFileType
    {
        Hex386,
        Hex86
    }

    public struct RecordType
    {
        public static string Data = "00";
        public static string EndOfFile = "01";
        public static string ExtendedSegmentAddress = "02";
        public static string StartSegmentAddress = "03";
        public static string ExtendedLinearAddress = "04";
        public static string StartLinearAddress = "05";
    }
}