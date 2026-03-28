namespace QRCoder;

public partial class QRCodeGenerator
{
    private readonly struct CodewordBlock
    {
        public CodewordBlock(int codeWordsOffset, int codeWordsLength, ArraySegment<byte> eccWords)
        {
            CodeWordsOffset = codeWordsOffset;
            CodeWordsLength = codeWordsLength;
            ECCWords = eccWords;
        }
        public int CodeWordsOffset { get; }
        public int CodeWordsLength { get; }
        public ArraySegment<byte> ECCWords { get; }
        private static List<CodewordBlock>? _codewordBlocks;
        public static List<CodewordBlock> GetList(int capacity)
            => Interlocked.Exchange(ref _codewordBlocks, null) ?? new List<CodewordBlock>(capacity);
        public static void ReturnList(List<CodewordBlock> list)
        {
            list.Clear();
            Interlocked.CompareExchange(ref _codewordBlocks, list, null);
        }
    }
}
