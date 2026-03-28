namespace QRCoder;

public partial class QRCodeGenerator
{
    private static class CapacityTables
    {
        private static readonly int[] _remainderBits = { 0, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0 };
        public static int GetRemainderBits(int version)
            => version < 0 ? 0 : _remainderBits[version - 1];
    }
}
