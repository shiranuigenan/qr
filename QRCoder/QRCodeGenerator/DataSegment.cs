using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private abstract class DataSegment
    {
        public string Text { get; }
        public abstract EncodingMode EncodingMode { get; }
        protected DataSegment(string text)
        {
            Text = text;
        }
        public abstract int WriteTo(BitArray bitArray, int startIndex, int version);
        public BitArray ToBitArray(int version)
        {
            var bitArray = new BitArray(GetBitLength(version));
            WriteTo(bitArray, 0, version);
            return bitArray;
        }
        public abstract int GetBitLength(int version);
    }
}
