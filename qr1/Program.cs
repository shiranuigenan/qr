using QRCoder;
using static QRCoder.QRCodeGenerator;

List<string> s266 = ["D*$5MT", "JZR*6K", "LYZ58F", ".92BD+", ":MEHE/"];

for (int i = 0; i < s266.Count; i++)
{
    using var qrCodeData = GenerateQrCode(s266[i], ECCLevel.H);
    Console.WriteLine(qrCodeData.Version);
    using var renderer = new PngByteQRCode(qrCodeData);
    File.WriteAllBytes($"266 {i}.png", renderer.GetGraphic(20));
}