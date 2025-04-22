#nullable enable
using System;
using System.Diagnostics;
using System.IO;

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
            var curPage = (int)(_realPosition / _pcf.PageSize);
            var maxPage = Math.Min((_realPosition + count) / _pcf.PageSize, _pcf.TotalPages - 1) ;           

            // for (long i = curPage; i <= maxPage; i++)
            // {            
            //     _pcf.EnsurePageLoadedByIdx((int)i);
            // }

            var realCount = (int)Math.Min(count, Length - _realPosition);

            var left = realCount;
            var dstPos = offset;
            var pageOffset = (int) (_realPosition % _pcf.PageSize);
            
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos = -1;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = offset;
                    break;
                case SeekOrigin.Current:
                    pos = _realPosition + offset;
                    break;
                case SeekOrigin.End:
                    pos = Length - offset;
                    break;
            }
            Debug.Assert(pos <= Length, $"pos {pos} len={Length}");
            // _pcf.EnsurePageLoadedByOffset(pos);
            _realPosition = pos;
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
            get => _realPosition;
            set => Seek(value, SeekOrigin.Begin);
        }



    }
}