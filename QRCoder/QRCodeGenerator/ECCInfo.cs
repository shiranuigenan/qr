namespace QRCoder;

public partial class QRCodeGenerator
{
    private struct ECCInfo
    {
        public ECCInfo(int version, int totalDataCodewords, int eccPerBlock, int blocksInGroup1,
            int codewordsInGroup1, int blocksInGroup2, int codewordsInGroup2)
        {
            Version = version;
            TotalDataCodewords = totalDataCodewords;
            TotalDataBits = totalDataCodewords * 8;
            ECCPerBlock = eccPerBlock;
            BlocksInGroup1 = blocksInGroup1;
            CodewordsInGroup1 = codewordsInGroup1;
            BlocksInGroup2 = blocksInGroup2;
            CodewordsInGroup2 = codewordsInGroup2;
        }
        public int Version { get; }
        public int TotalDataCodewords { get; }
        public int TotalDataBits { get; }
        public int ECCPerBlock { get; }
        public int BlocksInGroup1 { get; }
        public int CodewordsInGroup1 { get; }
        public int BlocksInGroup2 { get; }
        public int CodewordsInGroup2 { get; }
    }
}
