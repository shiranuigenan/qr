using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private sealed class AlphanumericDataSegment
    {
        public string Text { get; }
        public AlphanumericDataSegment(string alphanumericText)
        {
            Text=alphanumericText;
        }
        public int GetBitLength()
        {
            int modeIndicatorLength = 4;
            int countIndicatorLength = 9;
            int dataLength = AlphanumericEncoder.GetBitLength(Text.Length);
            int length = modeIndicatorLength + countIndicatorLength + dataLength;

            return length;
        }
        public int WriteTo(BitArray bitArray, int startIndex)
        {
            return WriteTo(Text, 0, Text.Length, bitArray, startIndex);
        }
        public static int WriteTo(string text, int offset, int length, BitArray bitArray, int bitIndex)
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

        public BitArray ToBitArray()
        {
            var bitArray = new BitArray(GetBitLength());
            WriteTo(bitArray, 0);
            return bitArray;
        }
    }
}
