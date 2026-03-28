using System.Collections;
using System.IO.Compression;

namespace QRCoder;

public class QRCodeData : IDisposable
{
    public List<BitArray> ModuleMatrix { get; set; }
    public QRCodeData(int version, bool addPadding)
    {
        Version = version;
        var size = ModulesPerSideFromVersion(version) + (addPadding ? 8 : 0);
        ModuleMatrix = new List<BitArray>(size);
        for (var i = 0; i < size; i++)
            ModuleMatrix.Add(new BitArray(size));
    }
    public int Version { get; private set; }
    private static int ModulesPerSideFromVersion(int version)
        => version > 0
            ? 21 + (version - 1) * 4
            : 11 + (-version - 1) * 2;
    public virtual void Dispose()
    {
        ModuleMatrix = null!;
        Version = 0;
        GC.SuppressFinalize(this);
    }
}
