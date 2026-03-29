using System.Collections;
using System.IO.Compression;

namespace QRCoder;

public class QRCodeData : IDisposable
{
    public List<BitArray> ModuleMatrix { get; set; }
    public QRCodeData(int version, bool addPadding)
    {
        Version = version;
        var size = 21 + (addPadding ? 8 : 0);
        ModuleMatrix = new List<BitArray>(size);
        for (var i = 0; i < size; i++)
            ModuleMatrix.Add(new BitArray(size));
    }
    public int Version { get; private set; }
    public virtual void Dispose()
    {
        ModuleMatrix = null!;
        Version = 0;
        GC.SuppressFinalize(this);
    }
}
