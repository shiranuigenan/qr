using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private sealed class AlphanumericDataSegment
    {
        public string Text { get; }
        public AlphanumericDataSegment(string alphanumericText)
        {
            Text = alphanumericText;
        }
        public int GetBitLength(int version)
        {
            return GetBitLength(Text.Length, version);
        }
        public static int GetBitLength(int textLength, int version)
        {
            int modeIndicatorLength = 4;
            int countIndicatorLength = 9;
            int dataLength = AlphanumericEncoder.GetBitLength(textLength);
            int length = modeIndicatorLength + countIndicatorLength + dataLength;

            return length;
        }
        public int WriteTo(BitArray bitArray, int startIndex, int version)
        {
            return WriteTo(Text, 0, Text.Length, bitArray, startIndex, version);
        }
        public static int WriteTo(string text, int offset, int length, BitArray bitArray, int bitIndex, int version)
        {
            // write mode indicator
            bitIndex = DecToBin(2, 4, bitArray, bitIndex);

            // write count indicator
            int countIndicatorLength = 9;
            bitIndex = DecToBin(length, countIndicatorLength, bitArray, bitIndex);

            // write data - encode alphanumeric text
            bitIndex = AlphanumericEncoder.WriteToBitArray(text, offset, length, bitArray, bitIndex);

            return bitIndex;
        }

        public BitArray ToBitArray(int version)
        {
            var bitArray = new BitArray(GetBitLength(version));
            WriteTo(bitArray, 0, version);
            return bitArray;
        }
    }
}
