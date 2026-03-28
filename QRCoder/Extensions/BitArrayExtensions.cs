using System.Collections;

namespace QRCoder;

internal static class BitArrayExtensions
{
    public static int CopyTo(this BitArray source, BitArray destination, int sourceOffset, int destinationOffset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            destination[destinationOffset + i] = source[sourceOffset + i];
        }
        return destinationOffset + count;
    }
}
