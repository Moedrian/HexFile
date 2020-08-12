namespace DotHex
{
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