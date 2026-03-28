namespace QRCoder;

public partial class QRCodeGenerator
{
    private struct PolynomItem
    {
        public PolynomItem(int coefficient, int exponent)
        {
            Coefficient = coefficient;
            Exponent = exponent;
        }
        public int Coefficient { get; }
        public int Exponent { get; }
    }
}
