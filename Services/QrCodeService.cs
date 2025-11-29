using QRCoder;

namespace PropertyInventory.Services;

public class QrCodeService
{
    public byte[] GenerateQrCode(string data, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        return qrCodeBytes;
    }

    public string GenerateQrCodeBase64(string data, int pixelsPerModule = 10)
    {
        var qrCodeBytes = GenerateQrCode(data, pixelsPerModule);
        return Convert.ToBase64String(qrCodeBytes);
    }
}