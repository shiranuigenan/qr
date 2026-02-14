using QRCoder;
using static QRCoder.QRCodeGenerator;

using var qrCodeData = GenerateQrCode("S:64 D", ECCLevel.H);
Console.WriteLine(qrCodeData.Version);
using var renderer = new PngByteQRCode(qrCodeData);
File.WriteAllBytes($"186.png", renderer.GetGraphic(512));