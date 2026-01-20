using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace MinRecall.Optimizer.ImageCompression;

public static class AvifCompressor
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static byte[]? CompressToAvif(Bitmap bitmap, int quality = 80)
    {
        try
        {
            if (bitmap == null)
                return null;

            // Convert System.Drawing.Bitmap to SKBitmap
            using var skBitmap = ConvertToSkiaBitmap(bitmap);
            if (skBitmap == null)
                return null;

            // Try AVIF first, fallback to WebP, then JPEG
            var avifData = CompressToAvifSkia(skBitmap, quality);
            if (avifData != null)
                return avifData;

            var webpData = CompressToWebPSkia(skBitmap, quality);
            if (webpData != null)
                return webpData;

            // Final fallback to JPEG
            return CompressToJpeg(bitmap, quality);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<byte[]?> CompressToAvifAsync(Bitmap bitmap, int quality = 80)
    {
        return await Task.Run(() => CompressToAvif(bitmap, quality));
    }

    private static byte[]? CompressToAvifSkia(SKBitmap skBitmap, int quality)
    {
        try
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Avif, quality);
            return encoded.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? CompressToWebPSkia(SKBitmap skBitmap, int quality)
    {
        try
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);
            return encoded.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static SKBitmap? ConvertToSkiaBitmap(Bitmap bitmap)
    {
        try
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixels = new byte[width * height * 4]; // RGBA

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            return SKBitmap.Decode(pixels);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] CompressToJpeg(Bitmap bitmap, int quality)
    {
        using var stream = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);

        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

        bitmap.Save(stream, jpegEncoder, encoderParams);
        return stream.ToArray();
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }

        return codecs[0];
    }

    public static Bitmap? DecompressFromBytes(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            
            // Try to decode as AVIF/WebP first
            var skBitmap = SKBitmap.Decode(stream);
            if (skBitmap != null)
            {
                return ConvertSkiaToBitmap(skBitmap);
            }

            // Fallback to System.Drawing
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap? ConvertSkiaToBitmap(SKBitmap skBitmap)
    {
        try
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<Bitmap?> DecompressFromBytesAsync(byte[] data)
    {
        return await Task.Run(() => DecompressFromBytes(data));
    }

    public static CompressionInfo GetCompressionInfo(byte[] compressedData)
    {
        try
        {
            var info = new CompressionInfo
            {
                OriginalSize = compressedData.Length,
                CompressionType = DetectCompressionType(compressedData),
                EstimatedQuality = EstimateQuality(compressedData),
                IsOptimized = true
            };

            return info;
        }
        catch
        {
            return new CompressionInfo
            {
                OriginalSize = compressedData.Length,
                CompressionType = "Unknown",
                EstimatedQuality = 0,
                IsOptimized = false
            };
        }
    }

    private static string DetectCompressionType(byte[] data)
    {
        if (data.Length < 12)
            return "Unknown";

        // Check for AVIF signature
        if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x00 && data[3] == 0x20 && 
            data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
            return "AVIF";

        // Check for WebP signature
        if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return "WebP";

        // Check for JPEG signature
        if (data[0] == 0xFF && data[1] == 0xD8)
            return "JPEG";

        return "Unknown";
    }

    private static int EstimateQuality(byte[] data)
    {
        // Simple quality estimation based on file size and compression type
        var type = DetectCompressionType(data);
        var size = data.Length;

        return type switch
        {
            "AVIF" => size < 50000 ? 90 : size < 100000 ? 80 : 70,
            "WebP" => size < 60000 ? 85 : size < 120000 ? 75 : 65,
            "JPEG" => size < 80000 ? 90 : size < 150000 ? 80 : 70,
            _ => 75
        };
    }
}

public class CompressionInfo
{
    public int OriginalSize { get; set; }
    public string CompressionType { get; set; } = string.Empty;
    public int EstimatedQuality { get; set; }
    public bool IsOptimized { get; set; }
    public double CompressionRatio { get; set; }
}
