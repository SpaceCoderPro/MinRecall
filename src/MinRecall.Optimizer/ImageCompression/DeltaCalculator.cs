using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MinRecall.Optimizer.ImageCompression;

public class DeltaCalculator
{
    public class DeltaRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] PixelData { get; set; } = Array.Empty<byte>();
    }

    public class DeltaFile
    {
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public List<DeltaRegion> Regions { get; set; } = new();
    }

    public static DeltaFile? CalculateDelta(Bitmap current, Bitmap previous)
    {
        try
        {
            if (current.Width != previous.Width || current.Height != previous.Height)
            {
                return null; // Can't calculate delta if dimensions differ
            }

            var delta = new DeltaFile
            {
                OriginalWidth = current.Width,
                OriginalHeight = current.Height
            };

            // Lock both bitmaps for fast pixel access
            var currentData = current.LockBits(
                new Rectangle(0, 0, current.Width, current.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            var previousData = previous.LockBits(
                new Rectangle(0, 0, previous.Width, previous.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            var currentBytes = new byte[Math.Abs(currentData.Stride) * current.Height];
            var previousBytes = new byte[Math.Abs(previousData.Stride) * previous.Height];

            Marshal.Copy(currentData.Scan0, currentBytes, 0, currentBytes.Length);
            Marshal.Copy(previousData.Scan0, previousBytes, 0, previousBytes.Length);

            // Find regions that changed
            var blockSize = 16; // 16x16 pixel blocks
            var regions = new List<DeltaRegion>();

            for (int y = 0; y < current.Height; y += blockSize)
            {
                for (int x = 0; x < current.Width; x += blockSize)
                {
                    if (HasBlockChanged(x, y, blockSize, currentBytes, previousBytes, current.Width, currentData.Stride))
                    {
                        var blockHeight = Math.Min(blockSize, current.Height - y);
                        var blockWidth = Math.Min(blockSize, current.Width - x);

                        var pixelData = ExtractBlock(x, y, blockWidth, blockHeight, currentBytes, currentData.Stride);

                        regions.Add(new DeltaRegion
                        {
                            X = x,
                            Y = y,
                            Width = blockWidth,
                            Height = blockHeight,
                            PixelData = pixelData
                        });
                    }
                }
            }

            current.UnlockBits(currentData);
            previous.UnlockBits(previousData);

            delta.Regions = regions;
            return delta;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasBlockChanged(int startX, int startY, int blockSize,
        byte[] currentBytes, byte[] previousBytes, int width, int stride)
    {
        var tolerance = 10; // Pixel difference threshold
        var changedPixels = 0;
        var totalPixels = 0;

        for (int y = startY; y < startY + blockSize && y < currentBytes.Length / Math.Abs(stride); y++)
        {
            for (int x = startX; x < startX + blockSize && x < width; x++)
            {
                var offset = y * Math.Abs(stride) + x * 3;

                if (offset + 2 < currentBytes.Length)
                {
                    var rDiff = Math.Abs(currentBytes[offset] - previousBytes[offset]);
                    var gDiff = Math.Abs(currentBytes[offset + 1] - previousBytes[offset + 1]);
                    var bDiff = Math.Abs(currentBytes[offset + 2] - previousBytes[offset + 2]);

                    if (rDiff + gDiff + bDiff > tolerance * 3)
                    {
                        changedPixels++;
                    }

                    totalPixels++;
                }
            }
        }

        // Consider block changed if more than 5% of pixels are different
        return totalPixels > 0 && ((float)changedPixels / totalPixels) > 0.05f;
    }

    private static byte[] ExtractBlock(int startX, int startY, int blockWidth, int blockHeight,
        byte[] sourceBytes, int stride)
    {
        var blockData = new byte[blockWidth * blockHeight * 3];
        var destIndex = 0;

        for (int y = startY; y < startY + blockHeight; y++)
        {
            for (int x = startX; x < startX + blockWidth; x++)
            {
                var offset = y * Math.Abs(stride) + x * 3;

                if (offset + 2 < sourceBytes.Length)
                {
                    blockData[destIndex++] = sourceBytes[offset];
                    blockData[destIndex++] = sourceBytes[offset + 1];
                    blockData[destIndex++] = sourceBytes[offset + 2];
                }
                else
                {
                    // Pad with black if out of bounds
                    blockData[destIndex++] = 0;
                    blockData[destIndex++] = 0;
                    blockData[destIndex++] = 0;
                }
            }
        }

        return blockData;
    }

    public static Bitmap? ApplyDelta(Bitmap keyframe, DeltaFile delta)
    {
        try
        {
            var result = new Bitmap(keyframe);

            for (int i = 0; i < result.Width; i++)
            {
                for (int j = 0; j < result.Height; j++)
                {
                    result.SetPixel(i, j, keyframe.GetPixel(i, j));
                }
            }

            foreach (var region in delta.Regions)
            {
                // Apply delta region
                for (int y = 0; y < region.Height; y++)
                {
                    for (int x = 0; x < region.Width; x++)
                    {
                        var pixelIndex = (y * region.Width + x) * 3;
                        if (pixelIndex + 2 < region.PixelData.Length)
                        {
                            var color = Color.FromArgb(
                                region.PixelData[pixelIndex],
                                region.PixelData[pixelIndex + 1],
                                region.PixelData[pixelIndex + 2]
                            );
                            result.SetPixel(region.X + x, region.Y + y, color);
                        }
                    }
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    public static byte[] SerializeDelta(DeltaFile delta)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(delta.OriginalWidth);
        writer.Write(delta.OriginalHeight);
        writer.Write(delta.Regions.Count);

        foreach (var region in delta.Regions)
        {
            writer.Write(region.X);
            writer.Write(region.Y);
            writer.Write(region.Width);
            writer.Write(region.Height);
            writer.Write(region.PixelData.Length);
            writer.Write(region.PixelData);
        }

        return stream.ToArray();
    }

    public static DeltaFile? DeserializeDelta(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var delta = new DeltaFile
            {
                OriginalWidth = reader.ReadInt32(),
                OriginalHeight = reader.ReadInt32()
            };

            var regionCount = reader.ReadInt32();
            for (int i = 0; i < regionCount; i++)
            {
                var region = new DeltaRegion
                {
                    X = reader.ReadInt32(),
                    Y = reader.ReadInt32(),
                    Width = reader.ReadInt32(),
                    Height = reader.ReadInt32(),
                    PixelData = reader.ReadBytes(reader.ReadInt32())
                };
                delta.Regions.Add(region);
            }

            return delta;
        }
        catch
        {
            return null;
        }
    }
}
