#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SickSharp.Format
{
    public sealed class NonAllocPageCachedStream : Stream, ICachedStream
    {
        private long _realPosition;
        private readonly PageCachedFile _pcf;

        public NonAllocPageCachedStream(PageCachedFile file)
        {
            _pcf = file;
            Length = _pcf.Length;
            _realPosition = 0;
        }

        ~NonAllocPageCachedStream()
        {
            Dispose(false);
        }

        public double CacheSaturation()
        {
            return _pcf.CacheSaturation();
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            var pos = Volatile.Read(ref _realPosition);
            var curPage = (int)(pos / _pcf.PageSize);
            var maxPage = Math.Min((pos + count) / _pcf.PageSize, _pcf.TotalPages - 1);

            // for (long i = curPage; i <= maxPage; i++)
            // {            
            //     _pcf.EnsurePageLoadedByIdx((int)i);
            // }

            var realCount = (int)Math.Min(count, Length - pos);

            var left = realCount;
            var dstPos = offset;
            var pageOffset = (int)(pos % _pcf.PageSize);

            for (int i = curPage; i <= maxPage; i++)
            {
                var page = _pcf.GetPage(i);
                var toRead = Math.Min(left, _pcf.PageSize - pageOffset);

                var dest = buffer.AsSpan(dstPos, toRead);
                var src = page.AsSpan(pageOffset, toRead);
                src.CopyTo(dest);

                dstPos += toRead;
                left -= toRead;
                pageOffset = 0;
            }

            Position += realCount;
            return realCount;
        }

        public ReadOnlySpan<byte> ReadSpanDirect(int offset, int count)
        {
            var pos = offset;
            var curPage = pos / _pcf.PageSize;
            var maxPage = Math.Min((pos + count) / _pcf.PageSize, _pcf.TotalPages - 1);

            var realCount = (int)Math.Min(count, Length - pos);
            if (realCount <= 0)
                return ReadOnlySpan<byte>.Empty;

            Position = offset + realCount;

            var pageOffset = pos % _pcf.PageSize;

            // requested data fits into one page
            if (curPage == maxPage)
            {
                var page = _pcf.GetPage(curPage);
                var toRead = Math.Min(realCount, _pcf.PageSize - pageOffset);
                return new ReadOnlySpan<byte>(page, pageOffset, toRead);
            }

            // Our data spans across several pages, bad case
            var buffer = new byte[realCount];
            var dstPos = 0;
            var remaining = realCount;

            for (var i = curPage; i <= maxPage && remaining > 0; i++)
            {
                var page = _pcf.GetPage(i);
                var copySize = Math.Min(remaining, _pcf.PageSize - pageOffset);

                new ReadOnlySpan<byte>(page, pageOffset, copySize).CopyTo(
                    buffer.AsSpan(dstPos, copySize));

                dstPos += copySize;
                remaining -= copySize;
                pageOffset = 0; // Reset offset after first page
            }

            return new ReadOnlySpan<byte>(buffer, 0, realCount);
        }

        public ReadOnlyMemory<byte> ReadMemoryDirect(int offset, int count)
        {
            var pos = offset;
            var curPage = pos / _pcf.PageSize;
            var maxPage = Math.Min((pos + count) / _pcf.PageSize, _pcf.TotalPages - 1);

            var realCount = (int)Math.Min(count, Length - pos);
            if (realCount <= 0)
                return ReadOnlyMemory<byte>.Empty;

            Position = offset + realCount;

            var pageOffset = pos % _pcf.PageSize;

            // requested data fits into one page
            if (curPage == maxPage)
            {
                var page = _pcf.GetPage(curPage);
                var toRead = Math.Min(realCount, _pcf.PageSize - pageOffset);
                return new ReadOnlyMemory<byte>(page, pageOffset, toRead);
            }

            // Our data spans across several pages, bad case
            var buffer = new byte[realCount];
            var dstPos = 0;
            var remaining = realCount;

            for (var i = curPage; i <= maxPage && remaining > 0; i++)
            {
                var page = _pcf.GetPage(i);
                var copySize = Math.Min(remaining, _pcf.PageSize - pageOffset);

                new ReadOnlySpan<byte>(page, pageOffset, copySize).CopyTo(
                    buffer.AsSpan(dstPos, copySize));

                dstPos += copySize;
                remaining -= copySize;
                pageOffset = 0; // Reset offset after first page
            }

            return new ReadOnlyMemory<byte>(buffer, 0, realCount);
        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos = -1;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = offset;
                    break;
                case SeekOrigin.Current:
                    pos = Volatile.Read(ref _realPosition) + offset;
                    break;
                case SeekOrigin.End:
                    pos = Length - offset;
                    break;
            }

            Debug.Assert(pos <= Length, $"pos {pos} len={Length}");
            // _pcf.EnsurePageLoadedByOffset(pos);
            Volatile.Write(ref _realPosition, pos);
            return _realPosition;
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }


        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position
        {
            get => Volatile.Read(ref _realPosition);
            set => Volatile.Write(ref _realPosition, value);
        }
    }
}