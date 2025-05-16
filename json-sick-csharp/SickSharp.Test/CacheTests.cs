using System.Diagnostics;
using System.Security.Cryptography;
using SickSharp.IO;
using SickSharp.Primitives;

namespace SickSharp.Test;

public class CacheTests
{
    private readonly long _fileSize = 1 * 1024 * 1024 + 300;


    public static void WriteRandomFile(string filePath, long sizeInBytes)
    {
        using (var rng = RandomNumberGenerator.Create())
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] buffer = new byte[8192]; // Buffer size of 8KB.
            long bytesRemaining = sizeInBytes;
            while (bytesRemaining > 0)
            {
                int bytesToWrite = (int)Math.Min(buffer.Length, bytesRemaining);
                rng.GetBytes(buffer, 0, bytesToWrite);
                fs.Write(buffer, 0, bytesToWrite);
                bytesRemaining -= bytesToWrite;
            }
        }
    }

    public static byte[] ReadBytesFromFileStream(Stream fileStream, long offset, int size)
    {
        if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
        if (offset < 0 || offset >= fileStream.Length) throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of file bounds.");
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");

        int bytesToRead = size; //(int)Math.Min(size, fileStream.Length - offset);
        byte[] buffer = new byte[bytesToRead];

        fileStream.Seek(offset, SeekOrigin.Begin);
        int bytesRead = fileStream.ReadUpTo(buffer, 0, bytesToRead);
        Debug.Assert(bytesRead == size);
        return buffer;
    }


    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void Test_NonallocCache()
    {
        var tempFilePath = Path.GetTempFileName();
        WriteRandomFile(tempFilePath, _fileSize);

        var f1 = File.Open(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var f2 = new NonAllocPageCachedStream(new PageCachedFile(tempFilePath, 4192, ISickProfiler.Noop()));

        CheckCorrectness(f1, f2);

        CheckCopy(f2);
    }

    private static void CheckCopy(Stream f2)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            f2.Seek(0, SeekOrigin.Begin);
            f2.CopyTo(ms);
        }
    }

    public void CheckCorrectness(Stream f1, Stream f2)
    {
        var rng = new Random();

        CompareSpans(f1, f2, 0, 4191);
        CompareSpans(f1, f2, 0, 4192);
        CompareSpans(f1, f2, 0, 4193);
        CompareSpans(f1, f2, 1, 4191);
        CompareSpans(f1, f2, 1, 4192);
        CompareSpans(f1, f2, 1, 4193);

        CompareSpans(f1, f2, 4191, 8000);
        CompareSpans(f1, f2, 4192, 8000);
        CompareSpans(f1, f2, 4193, 8000);

        CompareSpans(f1, f2, 4191, 300);
        CompareSpans(f1, f2, 4192, 300);
        CompareSpans(f1, f2, 4193, 300);

        CompareSpans(f1, f2, 4191, 30000);
        CompareSpans(f1, f2, 4192, 30000);
        CompareSpans(f1, f2, 4193, 30000);

        CompareSpans(f1, f2, _fileSize - 299, 299);
        CompareSpans(f1, f2, _fileSize - 300, 300);
        CompareSpans(f1, f2, _fileSize - 301, 301);

        CompareSpans(f1, f2, _fileSize - 4192, 4192);
        CompareSpans(f1, f2, _fileSize - 4193, 4192);

        CompareSpans(f1, f2, _fileSize - 16383, 16383);
        CompareSpans(f1, f2, _fileSize - 16384, 16384);
        CompareSpans(f1, f2, _fileSize - 16385, 16385);


        for (int i = 0; i < 10000; i++)
        {
            var offset = rng.NextInt64(0, _fileSize - 1);
            var count = rng.Next(1, (int)(_fileSize - offset - 1));

            CompareSpans(f1, f2, offset, count);
        }
    }

    private static void CompareSpans(Stream f1, Stream f2, long offset, int count)
    {
        var a1 = ReadBytesFromFileStream(f1, offset, count);
        var a2 = ReadBytesFromFileStream(f2, offset, count);
        Assert.IsTrue(a1.SequenceEqual(a2), $"offset={offset} count={count}");
    }
}