using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DotHex
{

    public enum HexFileType
    {
        Hex386,
        Hex86
    }

    public enum RecordType
    {
        Data,                   // = "00"
        EndOfFile,              // = "01"
        ExtendedSegmentAddress, // = "02"
        StartSegmentAddress,    // = "03"
        ExtendedLinearAddress,  // = "04"
        StartLinearAddress      // = "05"
    }

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
            var lineCtr = 1;
            var candidateLines = new Dictionary<RecordLine, int>();

            int GetLineNumberFromCandidates(Dictionary<RecordLine, int> candidateRecords, int intLineAddress)
            {
                foreach (var candidate in candidateRecords)
                {
                    var startAddress = int.Parse(candidate.Key.Address, NumberStyles.HexNumber);
                    var endAddress = startAddress + candidate.Key.DataLength;
                    if (intLineAddress >= startAddress && intLineAddress <= endAddress)
                        return candidate.Value;
                }

                return 0;
            }

            if (hexAdr.Length == 4)
            {
                var firstTwoDigit = hexAdr.Substring(0, 2);

                foreach (var line in File.ReadLines(_hexFilename))
                {
                    var record = new RecordLine(line);
                    if (record.Address.Substring(0, 2) == firstTwoDigit)
                        candidateLines.Add(record, lineCtr);
                    lineCtr++;
                }

                var intAdr = int.Parse(hexAdr, NumberStyles.HexNumber);

                return GetLineNumberFromCandidates(candidateLines, intAdr);
            }
            
            if (hexAdr.Length == 8)
            {
                var extendedAddress = hexAdr.Substring(0, 4);
                var lineAdrFirstTwoDigits = hexAdr.Substring(4, 2);
                var lineAdr = hexAdr.Substring(4, 4);

                var extAdrLine = GenerateHexLine(SpecialRecAdr, RecordType.ExtendedLinearAddress, extendedAddress);

                var foundFlag = false;

                var intAdr = int.Parse(lineAdr, NumberStyles.HexNumber);

                foreach (var line in File.ReadLines(_hexFilename))
                {
                    // Find the line describing line extension
                    if (line == extAdrLine)
                    {
                        foundFlag = true;
                    }

                    // Continue to find the address
                    if (foundFlag)
                    {
                        var record = new RecordLine(line);
                        if (record.Address.Substring(0, 2) == lineAdrFirstTwoDigits &&
                            record.RecordType == RecordType.Data)
                        {
                            candidateLines.Add(record, lineCtr);
                            continue;
                        }
                        if (record.RecordType != RecordType.Data && line != extAdrLine)
                            break;
                    }

                    lineCtr++;
                }

                return GetLineNumberFromCandidates(candidateLines, intAdr);
            }

            return 0;
        }


        private class RecordLine
        {
            public readonly int DataLength;
            public readonly string Address;
            public readonly RecordType RecordType;
            public string Data;

            public RecordLine(string dataLine)
            {
                Address = dataLine.Substring(3, 4);
                RecordType = ReflectRecordType(dataLine.Substring(7, 2));
                Data = dataLine.Substring(LineStartOffset, DataLength * 2);
                DataLength = int.Parse(dataLine.Substring(1, 2), NumberStyles.HexNumber);
            }
        }


        public static string GenerateHexLine(string address, RecordType recordType, string data)
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
            hexValueString.Append(GetRecordType(recordType));

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

            var binaryString = Convert.ToString(i, 2);

            // Flip Zero and One
            var onesComplement = new StringBuilder();
            foreach (var bit in binaryString)
                onesComplement.Append(bit == '0' ? '1' : '0');

            // Get Two's Complement
            var twosComplement = Convert.ToInt32(onesComplement.ToString(), 2) + 1;

            var bytes = BitConverter.GetBytes(twosComplement);
            var bytesArray = BitConverter.ToString(bytes).Split('-');
            // Little Endian
            var checkSum = bytesArray[0];

            if (checkSum == "F")
                checkSum = "FF";

            return checkSum;
        }


        // SPAGHETTI TIME!!
        private static string GetRecordType(RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.Data:
                    return "00";
                case RecordType.EndOfFile:
                    return "01";
                case RecordType.ExtendedSegmentAddress:
                    return "02";
                case RecordType.StartSegmentAddress:
                    return "03";
                case RecordType.ExtendedLinearAddress:
                    return "04";
                case RecordType.StartLinearAddress:
                    return "05";
                default:
                    return "00";
            }
        }


        // SPAGHETTI TIME!! ENCORE!!!
        public static RecordType ReflectRecordType(string recordType)
        {
            switch (recordType)
            {
                case "00":
                    return RecordType.Data;
                case "01":
                    return RecordType.EndOfFile;
                case "02":
                    return RecordType.ExtendedSegmentAddress;
                case "03":
                    return RecordType.StartSegmentAddress;
                case "04":
                    return RecordType.ExtendedLinearAddress;
                case "05":
                    return RecordType.StartLinearAddress;
                default:
                    return RecordType.Data;
            }
        }
    }
}