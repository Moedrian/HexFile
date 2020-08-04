Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Text
Imports System

Module HexFile
    Public Function StringToHexValues(rawString As String) As String
        Dim charArray As Char() = rawString.ToCharArray()
        Dim hexValues As StringBuilder = new StringBuilder()
        For Each character As Char In charArray
            hexValues.Append((Convert.ToInt16(character).ToString("X")))
        Next

        Return hexValues.ToString()
    End Function

    
    Public Function HexLine(address As String, data As String, Optional recordType As String = "00") As String
        Dim hexValues As List(Of String) = new List(Of String)

        ' : - StartCode
        Dim sb As StringBuilder = new StringBuilder(":")

        ' xx - Data byte count
        Dim hexLength As Integer = data.Length\2
        Dim byteCount As String = hexLength.ToString("X")
        If byteCount.Length Mod 2 <> 0
            byteCount = "0" + byteCount
        End If
        sb.Append(byteCount)
        hexValues.AddRange(GetByteHexValues(byteCount))

        ' xx - Address
        sb.Append(address)
        hexValues.AddRange(GetByteHexValues(address))

        ' xx - RecordType
        sb.Append(recordType)
        hexValues.Add(recordType)

        ' xx - Data
        sb.Append(data)
        hexValues.AddRange(GetByteHexValues(data))

        Dim i As Integer = 0

        For Each hexValue As String In hexValues
            If hexValue.Length <> 0
                i += StringToHex(hexValue)
            End If
        Next

        Dim checkSum As String = HexTwosComplement(i.ToString("X"))

        If checkSum = "F"
            checkSum = "FF"
        End If

        Return sb.ToString() & checkSum
    End Function


    Private Function StringToHex(value As String) As Byte
        Return Byte.Parse(value, NumberStyles.HexNumber)
    End Function


    Private Iterator Function GetByteHexValues(data As String) As IEnumerable(Of String)
        Const size As Integer = 2
        For i As Integer = 0 To data.Length Step size
            Yield data.Substring(i, Math.Min(size, data.Length - i))
        Next
    End Function


    Private Function FlipZeroAndOne(binaryString As String) As String
        ' 0 -> _
        Dim noZeroTemp As String = binaryString.Replace("0", "_")
        ' 1 -> 0
        Dim noOneTemp As String = noZeroTemp.Replace("1", "0")
        ' _ -> 1
        return noOneTemp.Replace("_", "1")
    End Function


    Private Function HexToBinary(hexValue As String) As String
        Dim charArray As Char() = hexValue.ToCharArray()
        Dim sb As StringBuilder = New StringBuilder()
        For Each character As Char In charArray
            sb.Append(Convert.ToString(Convert.ToInt32(character.ToString(), 16), 2).PadLeft(4, "0"c))
        Next

        Return sb.ToString()
    End Function


    Private Function HexLsbValue(value As Integer) As String
        Dim bytesArray As Byte() = BitConverter.GetBytes(value)
        Dim byteString As String() = BitConverter.ToString(bytesArray).Split("-"c)
        Return If(BitConverter.IsLittleEndian, byteString.First(), byteString.Last())
    End Function


    Private Function HexTwosComplement(hexValue As String) As String
        Dim onesComplement As String = FlipZeroAndOne(HexToBinary(hexValue))
        Dim twosComplement As Integer = Convert.ToInt32(onesComplement, 2) + 1
        Return HexLsbValue(twosComplement)
    End Function
End Module
