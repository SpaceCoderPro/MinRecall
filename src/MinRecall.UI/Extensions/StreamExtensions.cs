using System.IO;
using Windows.Storage.Streams;

namespace MinRecall.UI.Extensions;

public static class StreamExtensions
{
    public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
    {
        var randomAccessStream = new InMemoryRandomAccessStream();
        stream.CopyTo(randomAccessStream.AsStreamForWrite());
        randomAccessStream.Seek(0);
        return randomAccessStream;
    }
}
