﻿using System;
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


        /// <summary>
        /// Data replace from a certain address, supposing those data were locating in same extended part
        /// </summary>
        /// <param name="startPosition">Start Address of new data, length is 4 or 8, and this address is included.</param>
        /// <param name="data">Data that represents in hex number style.</param>
        public void Replace(string startPosition, string data)
        {
            var startPositionLineNumber = FindAbsAdrLnNum(startPosition);

            var endPositionLineNumber = 0;
            string endPositionLineAddress;

            if (startPosition.Length == 4)
            {
                endPositionLineAddress = Convert.ToString(int.Parse(startPosition, NumberStyles.HexNumber) + data.Length / 2, 16).PadLeft(4, '0');
                endPositionLineNumber = FindAbsAdrLnNum(endPositionLineAddress);
            }
            else if (startPosition.Length == 8)
            {
                endPositionLineAddress = Convert.ToString(int.Parse(startPosition.Substring(4, 4), NumberStyles.HexNumber) + data.Length / 2, 16).PadLeft(4, '0');
                var extendedAddress = startPosition.Substring(0, 4);
                endPositionLineNumber = FindAbsAdrLnNum(extendedAddress + endPositionLineAddress);
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

            // start position is included
            var modificationStart = modifiedLines.First().AddressList.IndexOf(startPosition.Length == 4 ? startPosition : startPosition.Substring(4, 4));
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


        private int FindAbsAdrLnNum(string hexAdr)
        {
            hexAdr = hexAdr.ToUpper();

            if (hexAdr.Length == 4)
            {
                var lnCtr = 1;
                foreach (var line in File.ReadLines(_hexFilename))
                {
                    var record = new RecordLine(line);
                    if (record.AddressList.Contains(hexAdr))
                        return lnCtr;

                    lnCtr++;
                }
            }
            else if (hexAdr.Length == 8)
            {
                var extendedAddress = hexAdr.Substring(0, 4);
                var lineAdr = hexAdr.Substring(4, 4);
                
                var extAdrLine = GenerateHexLine(SpecialRecAdr, RecordType.ExtendedLinearAddress, extendedAddress);
                
                var foundFlag = false;
                var lnCtr = 1;
                foreach (var line in File.ReadLines(_hexFilename))
                {
                    if (line.ToUpper() == extAdrLine)
                        foundFlag = true;

                    if (foundFlag)
                    {
                        var record = new RecordLine(line);
                        if (record.AddressList.Contains(lineAdr))
                            return lnCtr;
                    }

                    lnCtr++;
                }
            }
            return 0;
        }


        private static string GenerateHexLine(string address, RecordType recordType, string data)
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
            var checkSum = GetChecksum(hexValueString.ToString());

            return (StartCode + hexValueString + checkSum).ToUpper();
        }


        private static string GetChecksum(string hexValueString)
        {
            var hexByteValues = new List<string>(GetByteHexValues(hexValueString));

            // Hex Sum to Binary String
            var i = 0;
            foreach (var byteValue in hexByteValues)
                i += int.Parse(byteValue, NumberStyles.HexNumber);

            var onesComplement = ~i;
            var twosComplement = onesComplement + 1;

            var bytes = BitConverter.GetBytes(twosComplement);
            var bytesArray = BitConverter.ToString(bytes).Split('-');

            // Little Endian
            var checkSum = bytesArray.First().ToUpper();

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

            public List<string> AddressList
            {
                get
                {
                    var addresses = new List<string>();
                    for (var i = 0; i < this.DataLength; i++)
                    {
                        var byteAddress = Convert.ToString(int.Parse(this.Address, NumberStyles.HexNumber) + i, 16)
                            .PadLeft(4, '0');
                        addresses.Add(byteAddress.ToUpper());
                    }

                    return addresses;
                }
            }


            public RecordLine(string dataLine)
            {
                Address = dataLine.Substring(3, 4);
                RecordType = ReflectRecordType(dataLine.Substring(7, 2));
                DataLength = int.Parse(dataLine.Substring(1, 2), NumberStyles.HexNumber);
                Data = dataLine.Substring(LineStartOffset, DataLength * 2);
            }
        }


               // SPAGHETTI TIME!!
        private static string GetRecordType(RecordType recordType)
        {
            return recordType switch
            {
                RecordType.Data => "00",
                RecordType.EndOfFile => "01",
                RecordType.ExtendedSegmentAddress => "02",
                RecordType.StartSegmentAddress => "03",
                RecordType.ExtendedLinearAddress => "04",
                RecordType.StartLinearAddress => "05",
                _ => "00"
            };
        }


        // SPAGHETTI TIME!! ENCORE!!!
        private static RecordType ReflectRecordType(string recordType)
        {
            return recordType switch
            {
                "00" => RecordType.Data,
                "01" => RecordType.EndOfFile,
                "02" => RecordType.ExtendedSegmentAddress,
                "03" => RecordType.StartSegmentAddress,
                "04" => RecordType.ExtendedLinearAddress,
                "05" => RecordType.StartLinearAddress,
                _ => RecordType.Data
            };
        }

    }
}