namespace QRCoder;

public partial class QRCodeGenerator
{
    internal enum EncodingMode
    {
        Numeric = 1,
        Alphanumeric = 2,
        Byte = 4,
        Kanji = 8,
        ECI = 7
    }
}
