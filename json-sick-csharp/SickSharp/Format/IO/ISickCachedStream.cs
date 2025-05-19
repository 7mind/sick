using System;

namespace SickSharp.IO
{
    public interface ICachedStream : IDisposable
    {
        public double CacheSaturation();
        public ReadOnlySpan<byte> ReadSpan(int offset, int count);
        public ReadOnlyMemory<byte> ReadMemory(int offset, int count);
        public void Close();
    }
}