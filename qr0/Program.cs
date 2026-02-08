using System.Collections;
using static QRCoder.QRCodeGenerator;

if (args.Length < 1)
    return;

var z = Convert.ToByte(args[0]);

if (z < 0 || z > 44) return;

var ch = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

var t = DateTime.Now;
Console.WriteLine(t);

var sz = "" + ch[z];
for (byte a = 0; a < 45; a++)
{
    var sa = sz + ch[a];
    for (byte b = 0; b < 45; b++)
    {
        var sb = sa + ch[b];
        for (byte c = 0; c < 45; c++)
        {
            var sc = sb + ch[c];
            for (byte d = 0; d < 45; d++)
            {
                var sd = sc + ch[d];
                for (byte e = 0; e < 45; e++)
                {
                    var se = sd + ch[e];

                    using var qrCode = GenerateQrCode(se, ECCLevel.H);
                    var x = OneCount(qrCode.ModuleMatrix);

                    if (x < 191)
                        Console.WriteLine($"{x} {se}");
                    if (x > 261)
                        Console.WriteLine($"{x} {se}");
                }
            }
        }
    }
}

Console.WriteLine(DateTime.Now);
Console.WriteLine(DateTime.Now - t);
Console.WriteLine();

short OneCount(List<BitArray> matrix)
{
    short count = 0;
    for (int i = 4; i < 25; i++)
        for (int j = 4; j < 25; j++)
            if (matrix[i][j]) count++;
    return count;
}
