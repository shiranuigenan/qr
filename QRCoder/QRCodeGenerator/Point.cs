using System.Reflection;

namespace QRCoder;

public partial class QRCodeGenerator
{
    private readonly struct Point : IEquatable<Point>
    {
        public int X { get; }
        public int Y { get; }
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public bool Equals(Point other)
            => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is Point point && Equals(point);
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
