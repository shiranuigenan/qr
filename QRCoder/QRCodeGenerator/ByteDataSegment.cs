using System.Collections;
using System.Text;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private sealed class ByteDataSegment : DataSegment
    {
        public bool ForceUtf8 { get; }

        public bool Utf8BOM { get; }

        public EciMode EciMode { get; }

        public bool HasEciMode => EciMode != EciMode.Default;

        public override EncodingMode EncodingMode => EncodingMode.Byte;

        public ByteDataSegment(string text, bool forceUtf8, bool utf8BOM, EciMode eciMode)
            : base(text)
        {
            ForceUtf8 = forceUtf8;
            Utf8BOM = utf8BOM;
            EciMode = eciMode;
        }

        public override int GetBitLength(int version)
        {
            int modeIndicatorLength = HasEciMode ? 16 : 4;
            int countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            int dataBitLength = GetPlainTextToBinaryByteBitLength(Text, EciMode, Utf8BOM, ForceUtf8);
            int length = modeIndicatorLength + countIndicatorLength + dataBitLength;

            return length;
        }

        public override int WriteTo(BitArray bitArray, int startIndex, int version)
        {
            var index = startIndex;

            // write eci mode if present
            if (HasEciMode)
            {
                index = DecToBin((int)EncodingMode.ECI, 4, bitArray, index);
                index = DecToBin((int)EciMode, 8, bitArray, index);
            }

            // write mode indicator
            index = DecToBin((int)EncodingMode.Byte, 4, bitArray, index);

            // write count indicator
            int dataBitLength = GetPlainTextToBinaryByteBitLength(Text, EciMode, Utf8BOM, ForceUtf8);
            int characterCount = dataBitLength / 8;
            int countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Byte);
            index = DecToBin(characterCount, countIndicatorLength, bitArray, index);

            // write data directly to the bit array
            index = PlainTextToBinaryByte(Text, EciMode, Utf8BOM, ForceUtf8, bitArray, index);

            return index;
        }
    }
    private static readonly Encoding _iso8859_1 = Encoding.Latin1;
    private static Encoding? _iso8859_2;
    private static Encoding GetTargetEncoding(string plainText, EciMode eciMode, bool utf8BOM, bool forceUtf8, out bool includeUtf8BOM)
    {
        Encoding targetEncoding;

        // Check if the text is valid ISO-8859-1 and UTF-8 is not forced, then encode using ISO-8859-1.
        if (eciMode == EciMode.Default && !forceUtf8 && IsValidISO(plainText))
        {
            targetEncoding = _iso8859_1;
            includeUtf8BOM = false;
        }
        else
        {
            // Determine the encoding based on the specified ECI mode.
            switch (eciMode)
            {
                case EciMode.Iso8859_1:
                    // Convert text to ISO-8859-1 and encode.
                    targetEncoding = _iso8859_1;
                    includeUtf8BOM = false;
                    break;
                case EciMode.Iso8859_2:
                    // Note: ISO-8859-2 is not natively supported on .NET Core
                    //
                    // Users must install the System.Text.Encoding.CodePages package and call Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                    // before using this encoding mode.
                    _iso8859_2 ??= Encoding.GetEncoding(28592); // ISO-8859-2
                    // Convert text to ISO-8859-2 and encode.
                    targetEncoding = _iso8859_2;
                    includeUtf8BOM = false;
                    break;
                case EciMode.Default:
                case EciMode.Utf8:
                default:
                    // Handle UTF-8 encoding, optionally adding a BOM if specified.
                    targetEncoding = Encoding.UTF8;
                    includeUtf8BOM = utf8BOM;
                    break;
            }
        }

        return targetEncoding;
    }
    private static int GetPlainTextToBinaryByteBitLength(string plainText, EciMode eciMode, bool utf8BOM, bool forceUtf8)
    {
        var targetEncoding = GetTargetEncoding(plainText, eciMode, utf8BOM, forceUtf8, out var includeUtf8BOM);
        int byteCount = targetEncoding.GetByteCount(plainText);
        return (byteCount * 8) + (includeUtf8BOM ? 24 : 0);
    }
    private static int PlainTextToBinaryByte(string plainText, EciMode eciMode, bool utf8BOM, bool forceUtf8, BitArray bitArray, int offset)
    {
        var targetEncoding = GetTargetEncoding(plainText, eciMode, utf8BOM, forceUtf8, out var includeUtf8BOM);

        byte[] codeBytes = targetEncoding.GetBytes(plainText);

        // Write the data to the BitArray
        if (includeUtf8BOM)
        {
            // write UTF8 preamble (EF BB BF) to the BitArray
            DecToBin(0xEF, 8, bitArray, offset);
            DecToBin(0xBB, 8, bitArray, offset + 8);
            DecToBin(0xBF, 8, bitArray, offset + 16);
            offset += 24;
        }
        CopyToBitArray(codeBytes, bitArray, offset);
        offset += (int)((uint)codeBytes.Length * 8);

        return offset;
    }
}
