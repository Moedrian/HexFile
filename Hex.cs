using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DotHex
{

    public enum HexFileType
    {
        Hex386,
        Hex86
    }


    // Record type of a certain line
    public enum RecordType
    {
        Data,                   // = "00"
        EndOfFile,              // = "01"
        ExtendedSegmentAddress, // = "02"
        StartSegmentAddress,    // = "03"
        ExtendedLinearAddress,  // = "04"
        StartLinearAddress      // = "05"
    }


    /// <summary>
    /// .hex File Utility
    /// </summary>
    public class Hex
    {
        private readonly HexFileType _hexFileType;
        private readonly string _hexFilename;

        private const string StartCode = ":";
        private const string SpecialRecAdr = "0000";

        // :LLAAAARR
        private const int LineStartOffset = 9;


        /// <summary>
        /// Default constructor of Hex
        /// </summary>
        /// <param name="hexFilename">Hex file to be parsed.</param>
        /// <param name="hexFileType">Default HEX386</param>
        public Hex(string hexFilename, HexFileType hexFileType = HexFileType.Hex386)
        {
            _hexFilename = hexFilename;
            _hexFileType = hexFileType;
        }


        // 
        /// <summary>
        /// Data replace from a certain address, supposing those data were locating in same extended part
        /// </summary>
        /// <param name="startPosition">Start Address of new data, length is 4 or 8, and this address is included.</param>
        /// <param name="data">Data that represents in hex number style.</param>
        public void Replace(string startPosition, string data)
        {
            var startPositionLineNumber = FindAbsAdrLineNumber(startPosition);

            var endPositionLineNumber = 0;
            string endPositionLineAddress;

            if (startPosition.Length == 4)
            {
                endPositionLineAddress = Convert.ToString(int.Parse(startPosition, NumberStyles.HexNumber) + data.Length / 2, 16).PadLeft(4, '0');
                endPositionLineNumber = FindAbsAdrLineNumber(endPositionLineAddress);
            }
            else if (startPosition.Length == 8)
            {
                endPositionLineAddress = Convert.ToString(int.Parse(startPosition.Substring(4, 4), NumberStyles.HexNumber) + data.Length / 2, 16).PadLeft(4, '0');
                var extendedAddress = startPosition.Substring(0, 4);
                endPositionLineNumber = FindAbsAdrLineNumber(extendedAddress + endPositionLineAddress);
            }

            var modifiedLines = new List<RecordLine>();

            var i = 1;
            foreach (var line in File.ReadLines(_hexFilename))
            {
                if (i >= startPositionLineNumber && i <= endPositionLineNumber)
                    modifiedLines.Add(new RecordLine(line));

                if (i == endPositionLineNumber)
                    break;

                i++;
            }

            var originalData = string.Join(string.Empty, modifiedLines.Select(o => o.Data));
            var originalHexData = GetByteHexValues(originalData).ToArray();

            var newDataSegment = GetByteHexValues(data).ToArray();

            // +1 -> start position is included
            var modificationStart = modifiedLines.First().AddressList().IndexOf(startPosition.Length == 4 ? startPosition : startPosition.Substring(4, 4)) + 1;
            var modificationEnd = modificationStart + newDataSegment.Length;

            // Replace original data with new data segment
            var modifiedHexData = new StringBuilder();
            var segmentCtr = 0;
            for (var j = 0; j < originalHexData.Length; j++)
            {
                if (j < modificationEnd && j >= modificationStart)
                {
                    modifiedHexData.Append(newDataSegment[segmentCtr]);
                    segmentCtr++;
                    continue;
                }
                modifiedHexData.Append(originalHexData[j]);
            }

            var modifiedHex = modifiedHexData.ToString();

            var newRecordLines = new List<string>();
            var offset = 0;
            foreach (var modifiedLine in modifiedLines)
            {
                var newLine = GenerateHexLine(modifiedLine.Address, modifiedLine.RecordType,
                    modifiedHex.Substring(offset, modifiedLine.DataLength * 2));
                newRecordLines.Add(newLine);
                offset += modifiedLine.DataLength * 2;
            }

            var copiedFile = _hexFilename + ".copy.hex";

            if (File.Exists(copiedFile))
                File.Delete(copiedFile);

            File.Copy(_hexFilename, copiedFile);

            var writeCtr = 1;
            var newLineCtr = 0;
            using (var sw = new StreamWriter(_hexFilename))
            {
                foreach (var line in File.ReadLines(copiedFile))
                {
                    if (writeCtr >= startPositionLineNumber && writeCtr <= endPositionLineNumber)
                    {
                        sw.WriteLine(newRecordLines[newLineCtr]);
                        newLineCtr++;
                        writeCtr++;
                        continue;
                    }
                    sw.WriteLine(line);
                    writeCtr++;
                }
            }

            File.Delete(copiedFile);
        }


        /// <summary>
        /// Find Absolute Address Line Number in given hex file.
        /// </summary>
        /// <param name="hexAdr">Address in hex style, length 4 or 8</param>
        /// <returns>Desired line number</returns>
        public int FindAbsAdrLineNumber(string hexAdr)
        {
            hexAdr = hexAdr.ToUpper();

            var lineCtr = 1;
            var candidateLines = new Dictionary<RecordLine, int>();

            if (hexAdr.Length == 4)
            {
                var firstTwoDigit = hexAdr.Substring(0, 2);

                foreach (var line in File.ReadLines(_hexFilename))
                {
                    var upperLine = line.ToUpper();
                    var record = new RecordLine(upperLine);
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
                    var upperLine = line.ToUpper();
                    // Find the line describing line extension
                    if (upperLine == extAdrLine)
                    {
                        foundFlag = true;
                    }

                    // Continue to find the address
                    if (foundFlag)
                    {
                        var record = new RecordLine(upperLine);
                        if (record.Address.Substring(0, 2) == lineAdrFirstTwoDigits &&
                            record.RecordType == RecordType.Data)
                        {
                            candidateLines.Add(record, lineCtr);
                            lineCtr++;
                            continue;
                        }
                        if (record.RecordType != RecordType.Data && upperLine != extAdrLine)
                            break;
                    }

                    lineCtr++;
                }

                return GetLineNumberFromCandidates(candidateLines, intAdr);
            }

            return 0;

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

            return (StartCode + hexValueString + checkSum).ToUpper();
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


        public static string ToHex(string charString)
        {
            var charArray = charString.ToCharArray();
            var hexStringBuilder = new StringBuilder();
            foreach (var character in charArray)
            {
                hexStringBuilder.Append(Convert.ToString(Convert.ToInt16(character), 16));
            }

            return hexStringBuilder.ToString();
        }


        private static IEnumerable<string> GetByteHexValues(string data)
        {
            const int size = 2;
            for (var i = 0; i < data.Length; i += size)
            {
                yield return data.Substring(i, Math.Min(size, data.Length - i));
            }
        }


        private class RecordLine
        {
            public readonly int DataLength;
            public readonly string Address;
            public readonly RecordType RecordType;
            public readonly string Data;


            public RecordLine(string dataLine)
            {
                Address = dataLine.Substring(3, 4);
                RecordType = ReflectRecordType(dataLine.Substring(7, 2));
                DataLength = int.Parse(dataLine.Substring(1, 2), NumberStyles.HexNumber);
                Data = dataLine.Substring(LineStartOffset, DataLength * 2);
            }


            public List<string> AddressList()
            {
                var addresses = new List<string>();
                for (var i = 1; i <= this.DataLength; i++)
                {
                    var byteAddress = Convert.ToString(int.Parse(this.Address, NumberStyles.HexNumber) + i, 16)
                        .PadLeft(4, '0');
                    addresses.Add(byteAddress);
                }

                return addresses;
            }
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