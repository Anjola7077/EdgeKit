
using System;
using System.IO;
using ZXing;
using ZXing.Common;
using Android.Graphics;

namespace EdgeKit;

internal static class BarcodeImageGenerator
{
    public static void WritePng(
        string value,
        string filePath,
        BarcodeFormat format,
        int width,
        int height,
        int margin = 3)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Barcode content cannot be empty.");

        var writer = new BarcodeWriterPixelData
        {
            Format = format,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = margin,
                PureBarcode = true
            }
        };

        var pixels = writer.Write(value);

        int pixelCount = pixels.Width * pixels.Height;
        int[] colors = new int[pixelCount];

        // ZXing returns pixels in BGRA byte order.
        // Android Bitmap.CreateBitmap expects ARGB integer pixels.
        for (int i = 0; i < pixelCount; i++)
        {
            int offset = i * 4;

            int b = pixels.Pixels[offset];
            int g = pixels.Pixels[offset + 1];
            int r = pixels.Pixels[offset + 2];
            int a = pixels.Pixels[offset + 3];

            colors[i] =
                (a << 24) |
                (r << 16) |
                (g << 8) |
                b;
        }

        using var bitmap = Bitmap.CreateBitmap(
            colors,
            pixels.Width,
            pixels.Height,
            Bitmap.Config.Argb8888!);

        using var stream = File.Create(filePath);

        bool saved = bitmap.Compress(
            Bitmap.CompressFormat.Png!,
            100,
            stream);

        if (!saved)
            throw new IOException("Android could not encode the barcode as PNG.");
    }
}
