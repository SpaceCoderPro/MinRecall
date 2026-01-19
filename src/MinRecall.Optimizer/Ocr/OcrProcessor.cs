using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace MinRecall.Optimizer.Ocr;

public static class OcrProcessor
{
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double? Confidence { get; set; }
    }

    public static async Task<OcrResult?> ExtractTextAsync(Bitmap bitmap, string language = "en-US")
    {
        try
        {
            // Convert bitmap to SoftwareBitmap
            var softwareBitmap = ConvertToSoftwareBitmap(bitmap);
            if (softwareBitmap == null)
            {
                return null;
            }

            // Initialize OCR engine
            var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(language));
            if (engine == null)
            {
                return null;
            }

            // Perform OCR
            var result = await engine.RecognizeAsync(softwareBitmap);
            softwareBitmap.Dispose();

            if (result == null)
            {
                return null;
            }

            var ocrResult = new OcrResult
            {
                Text = result.Text,
                Language = language,
                Confidence = CalculateConfidence(result)
            };

            return ocrResult;
        }
        catch
        {
            return null;
        }
    }

    private static SoftwareBitmap? ConvertToSoftwareBitmap(Bitmap bitmap)
    {
        try
        {
            // Lock bitmap data
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            var bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            var rgbValues = new byte[bytes];

            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);

            // Create SoftwareBitmap
            var softwareBitmap = new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                bitmap.Width,
                bitmap.Height
            );

            softwareBitmap.CopyToBuffer(rgbValues.AsBuffer());

            bitmap.UnlockBits(bmpData);

            return softwareBitmap;
        }
        catch
        {
            return null;
        }
    }

    private static double? CalculateConfidence(Windows.Media.Ocr.OcrResult result)
    {
        try
        {
            if (result.Lines.Count == 0)
            {
                return 0.0;
            }

            var totalConfidence = 0.0;
            var lineCount = 0;

            foreach (var line in result.Lines)
            {
                if (line.Words.Count > 0)
                {
                    var lineConfidence = line.Words.Average(w => w.Confidence);
                    totalConfidence += lineConfidence;
                    lineCount++;
                }
            }

            return lineCount > 0 ? totalConfidence / lineCount : 0.0;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> ExtractTextSimpleAsync(string imagePath, string language = "en-US")
    {
        try
        {
            var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(language));
            if (engine == null)
            {
                return null;
            }

            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(imagePath);
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync();

            var result = await engine.RecognizeAsync(bitmap);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<bool> IsAvailableAsync(string language = "en-US")
    {
        try
        {
            var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(language));
            return engine != null;
        }
        catch
        {
            return false;
        }
    }

    public static List<string> GetAvailableLanguages()
    {
        try
        {
            return OcrEngine.AvailableRecognizerLanguages
                .Select(l => l.LanguageTag)
                .ToList();
        }
        catch
        {
            return new List<string> { "en-US" };
        }
    }
}
