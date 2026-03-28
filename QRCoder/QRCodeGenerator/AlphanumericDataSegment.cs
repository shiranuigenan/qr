using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private sealed class AlphanumericDataSegment : DataSegment
    {
        public override EncodingMode EncodingMode => EncodingMode.Alphanumeric;
        public AlphanumericDataSegment(string alphanumericText)
            : base(alphanumericText)
        {
        }
        public override int GetBitLength(int version)
        {
            return GetBitLength(Text.Length, version);
        }
        public static int GetBitLength(int textLength, int version)
        {
            int modeIndicatorLength = 4;
            int countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Alphanumeric);
            int dataLength = AlphanumericEncoder.GetBitLength(textLength);
            int length = modeIndicatorLength + countIndicatorLength + dataLength;

            return length;
        }
        public override int WriteTo(BitArray bitArray, int startIndex, int version)
        {
            return WriteTo(Text, 0, Text.Length, bitArray, startIndex, version);
        }
        public static int WriteTo(string text, int offset, int length, BitArray bitArray, int bitIndex, int version)
        {
            // write mode indicator
            bitIndex = DecToBin((int)EncodingMode.Alphanumeric, 4, bitArray, bitIndex);

            // write count indicator
            int countIndicatorLength = GetCountIndicatorLength(version, EncodingMode.Alphanumeric);
            bitIndex = DecToBin(length, countIndicatorLength, bitArray, bitIndex);

            // write data - encode alphanumeric text
            bitIndex = AlphanumericEncoder.WriteToBitArray(text, offset, length, bitArray, bitIndex);

            return bitIndex;
        }
    }
}
