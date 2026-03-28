using System.Collections;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private static partial class ModulePlacer
    {
        public struct BlockedModules : IDisposable
        {
            private readonly BitArray[] _blockedModules;
            private static BitArray[]? _staticBlockedModules;
            public BlockedModules(int size)
            {
                _blockedModules = Interlocked.Exchange(ref _staticBlockedModules, null)!;
                if (_blockedModules != null && _blockedModules.Length >= size)
                {
                    for (int i = 0; i < size; i++)
                        _blockedModules[i].SetAll(false);
                }
                else
                {
                    _blockedModules = new BitArray[size];
                    for (int i = 0; i < size; i++)
                        _blockedModules[i] = new BitArray(size);
                }
            }
            public void Add(Rectangle rect)
            {
                for (int y = rect.Y; y < rect.Y + rect.Height; y++)
                {
                    for (int x = rect.X; x < rect.X + rect.Width; x++)
                    {
                        _blockedModules[y][x] = true;
                    }
                }
            }
            public bool IsBlocked(int x, int y)
                => _blockedModules[y][x];
            public bool IsBlocked(Rectangle r1)
            {
                for (int y = r1.Y; y < r1.Y + r1.Height; y++)
                {
                    for (int x = r1.X; x < r1.X + r1.Width; x++)
                    {
                        if (_blockedModules[y][x])
                            return true;
                    }
                }
                return false;
            }
            public void Dispose()
                => Interlocked.CompareExchange(ref _staticBlockedModules, _blockedModules, null);
        }
    }
}
