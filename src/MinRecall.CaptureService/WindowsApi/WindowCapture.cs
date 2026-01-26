using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MinRecall.CaptureService.WindowsApi;

[SupportedOSPlatform("windows")]
public static class WindowCapture
{
    #region Win32 API Imports

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSource, int xSrc, int ySrc, CopyPixelOperation rop);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr hWnd);

    #endregion

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [Flags]
    private enum CopyPixelOperation : uint
    {
        SRCCOPY = 0x00CC0020
    }

    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static WindowInfo? GetForegroundWindowInfo()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return null;

            var title = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);

            GetWindowThreadProcessId(hWnd, out var processId);
            var process = Process.GetProcessById((int)processId);

            GetWindowRect(hWnd, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            return new WindowInfo
            {
                Handle = hWnd,
                Title = title.ToString(),
                ProcessName = process.ProcessName,
                X = rect.Left,
                Y = rect.Top,
                Width = Math.Max(width, 1),
                Height = Math.Max(height, 1)
            };
        }
        catch
        {
            return null;
        }
    }

    public static Bitmap? CaptureWindow(IntPtr hWnd, int targetWidth, int targetHeight, int jpegQuality)
    {
        try
        {
            // Get window rect
            GetWindowRect(hWnd, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            // For fullscreen or maximized windows, capture the entire screen area
            var isMaximized = IsZoomed(hWnd);
            
            // Create bitmap with actual window dimensions
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Set high quality rendering
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            
            var hdc = graphics.GetHdc();
            try
            {
                // Try PrintWindow first (works better for most apps)
                var result = PrintWindow(hWnd, hdc, 0);
                
                // If PrintWindow fails, fallback to BitBlt
                if (!result)
                {
                    var hdcSource = GetDC(IntPtr.Zero);
                    BitBlt(hdc, 0, 0, width, height, hdcSource, rect.Left, rect.Top, CopyPixelOperation.SRCCOPY);
                    ReleaseDC(IntPtr.Zero, hdcSource);
                }
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
            
            // Resize if needed
            if (width != targetWidth || height != targetHeight)
            {
                var resized = new Bitmap(targetWidth, targetHeight);
                using var g = Graphics.FromImage(resized);
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
                bitmap.Dispose();
                return resized;
            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static Bitmap? CaptureWindowDirect(IntPtr hWnd)
    {
        try
        {
            GetWindowRect(hWnd, out var rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(bitmap);
            var hdc = graphics.GetHdc();

            PrintWindow(hWnd, hdc, 0);
            graphics.ReleaseHdc(hdc);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static MemoryStream SaveToJpeg(Bitmap bitmap, int quality)
    {
        var stream = new MemoryStream();
        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);

        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

        bitmap.Save(stream, jpegEncoder, encoderParams);
        return stream;
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

    public static bool IsDesktopWindow(IntPtr hWnd)
    {
        try
        {
            GetWindowThreadProcessId(hWnd, out var processId);
            var process = Process.GetProcessById((int)processId);
            return process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) &&
                   (hWnd == IntPtr.Zero || GetWindowText(hWnd, new System.Text.StringBuilder(256), 256) == 0);
        }
        catch
        {
            return false;
        }
    }
}
