Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Text
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module HexFile
    Public Function StringToHexValues(rawString As String) As String
        Dim charArray As Char() = rawString.ToCharArray()
        Dim hexValues As StringBuilder = new StringBuilder()
        For Each character As Char In charArray
            hexValues.Append((Convert.ToInt16(character).ToString("X")))
        Next

        Return hexValues.ToString()
    End Function

    Private Const StartCode As String = ":"

    Public Function HexLine(address As String, data As String, Optional recordType As String = "00") As String

        Dim hexValueString As StringBuilder = New StringBuilder()

        ' Data byte count
        Dim hexLength As Integer = data.Length \ 2
        Dim byteCount As String = hexLength.ToString("X")
        If byteCount.Length Mod 2 <> 0
            byteCount = "0" + byteCount
        End If
        hexValueString.Append(byteCount)

        ' Address
        hexValueString.Append(address)

        ' RecordType
        hexValueString.Append(recordType)

        ' Data
        hexValueString.Append(data)

        Dim checkSum As String = GetChecksum(hexValueString)

        Return StartCode & hexValueString.ToString() & checkSum
    End Function


    Private Function GetCheckSum(hexValueString As StringBuilder) As String

        Dim hexByteValues As List(Of String) = New List(Of String)(GetByteHexValues(hexValueString.ToString()))

        Dim i As Integer = 0

        For Each byteValue As String In hexByteValues
            If byteValue.Length <> 0
                i += Byte.Parse(byteValue, NumberStyles.HexNumber)
            End If
        Next

        ' To Binary String
        Dim charArray As Char() = i.ToString("X").ToCharArray()
        Dim binaryString As StringBuilder = New StringBuilder()
        For Each character As Char In charArray
            binaryString.Append(Convert.ToString(Convert.ToInt32(character.ToString(), 16), 2).PadLeft(4, "0"c))
        Next

        ' Flip Zero and One, One's Complement
        ' 0 -> _
        Dim noZeroTemp As String = binaryString.ToString().Replace("0", "_")
        ' 1 -> 0
        Dim noOneTemp As String = noZeroTemp.Replace("1", "0")
        ' _ -> 1
        Dim onesComplement As String = noOneTemp.Replace("_", "1")

        ' Two's Complement
        Dim twosComplement As Integer = Convert.ToInt32(onesComplement, 2) + 1

        Dim bytesArray As Byte() = BitConverter.GetBytes(twosComplement)
        Dim byteString As String() = BitConverter.ToString(bytesArray).Split("-"c)

        ' LSB Value aka checksum
        Dim checksum As String = If(BitConverter.IsLittleEndian, byteString.First(), byteString.Last())

        If checksum = "F" Then
            checksum = "FF"
        End If

        Return checksum

    End Function


    'Private Iterator Function GetByteHexValues(data As String) As IEnumerable(Of String)
    '    Const size As Integer = 2
    '    For i As Integer = 0 To data.Length Step size
    '        Yield data.Substring(i, Math.Min(size, data.Length - i))
    '    Next
    'End Function


    Private Function GetByteHexValues(data As String) As List(Of String)
        Const length As Integer = 2
        Dim hexValues As List(Of String) = New List(Of String)()

        For i As Integer = 0 To data.Length Step length
            hexValues.Add(data.Substring(i, Math.Min(length, data.Length - i)))
        Next
        
        Return hexValues
    End Function

End Module
