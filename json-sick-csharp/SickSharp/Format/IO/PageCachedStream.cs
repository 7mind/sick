#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SickSharp.IO
{
    public sealed class PageCachedStream : Stream, ICachedStream
    {
        private long _realPosition;
        private readonly PageCachedFile _pcf;

        public PageCachedStream(PageCachedFile file)
        {
            _pcf = file;
            Length = _pcf.Length;
            _realPosition = 0;
        }

        ~PageCachedStream()
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

        public ReadOnlySpan<byte> ReadSpan(int offset, int count)
        {
            var curPage = offset / _pcf.PageSize;
            var lastPage = Math.Min((offset + count) / _pcf.PageSize, _pcf.TotalPages - 1);

            count = (int)Math.Min(count, Length - offset);
            if (count <= 0) return ReadOnlySpan<byte>.Empty;

            Position = offset + count;

            var pageOffset = offset % _pcf.PageSize;

            // requested data fits into one page
            if (curPage == lastPage)
            {
                var copySize = Math.Min(count, _pcf.PageSize - pageOffset);
                return new ReadOnlySpan<byte>(_pcf.GetPage(curPage), pageOffset, copySize);
            }

            // Our data spans across several pages, bad case
            var buffer = new byte[count];
            var copied = 0;
            for (; curPage <= lastPage && count > 0; curPage++)
            {
                var copySize = Math.Min(count, _pcf.PageSize - pageOffset);

                new ReadOnlySpan<byte>(_pcf.GetPage(curPage), pageOffset, copySize)
                    .CopyTo(buffer.AsSpan(copied, copySize));

                copied += copySize;
                count -= copySize;
                pageOffset = 0; // Reset offset after first page
            }

            return new ReadOnlySpan<byte>(buffer, 0, buffer.Length);
        }

        public ReadOnlyMemory<byte> ReadMemory(int offset, int count)
        {
            var curPage = offset / _pcf.PageSize;
            var lastPage = Math.Min((offset + count) / _pcf.PageSize, _pcf.TotalPages - 1);

            count = (int)Math.Min(count, Length - offset);
            if (count <= 0) return ReadOnlyMemory<byte>.Empty;

            Position = offset + count;

            var pageOffset = offset % _pcf.PageSize;

            // requested data fits into one page
            if (curPage == lastPage)
            {
                var copySize = Math.Min(count, _pcf.PageSize - pageOffset);
                return new ReadOnlyMemory<byte>(_pcf.GetPage(curPage), pageOffset, copySize);
            }

            // Our data spans across several pages, bad case
            var buffer = new byte[count];
            var copied = 0;
            for (; curPage <= lastPage && count > 0; curPage++)
            {
                var copySize = Math.Min(count, _pcf.PageSize - pageOffset);

                new ReadOnlySpan<byte>(_pcf.GetPage(curPage), pageOffset, copySize)
                    .CopyTo(buffer.AsSpan(copied, copySize));

                copied += copySize;
                count -= copySize;
                pageOffset = 0; // Reset offset after first page
            }

            return new ReadOnlyMemory<byte>(buffer, 0, buffer.Length);
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
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
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