using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
            // For now, we'll use JPEG as a fallback since AVIF requires external libraries
            // In a production build, you would use libavif or similar library
            return CompressToJpeg(bitmap, quality);
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

    public static async Task<byte[]?> CompressToAvifAsync(Bitmap bitmap, int quality = 80)
    {
        return await Task.Run(() => CompressToAvif(bitmap, quality));
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
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
