using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SickSharp.Format
{
    public sealed class NonAllocPageCachedStream : Stream, ICachedStream
    {
        private long _realPosition;

        private readonly int _pageSize;
        private readonly byte[][] _buf;
        private readonly FileStream _underlying;
        private readonly long _totalPages;

        public NonAllocPageCachedStream(string path, int pageSize)
        {
            var info = new FileInfo(path);
            Length = info.Length;
            _realPosition = 0;
            _pageSize = pageSize;
            _totalPages = (Length + _pageSize - 1) / _pageSize;
            
            _buf = new byte[_totalPages][];
            
            for (int i = 0; i < _totalPages; i++)
            {
                Debug.Assert(_buf[i] == null);
            }
            
            _underlying = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        ~NonAllocPageCachedStream()
        {
            Dispose(false);
        }

        public double CacheSaturation()
        {
            var cc = 0.0;
            for (int i = 0; i < _totalPages; i++)
            {
                if (_buf[i] != null)
                {
                    cc += 1.0;
                }
            }
            return cc / _totalPages;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _underlying?.Dispose();
            }

            base.Dispose(disposing);
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (_underlying != null) await _underlying.DisposeAsync();
        }

        public sealed override async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var curPage = _realPosition / _pageSize;
            var maxPage = Math.Min((_realPosition + count + _pageSize - 1) / _pageSize, _totalPages) ;           
            //var maxPage = (_realPosition + count) / _pageSize;           

            for (long i = curPage; i <= maxPage; i++)
            {
                EnsurePageLoadedByIdx((int)i);
            }

            var realCount = (int)Math.Min(count, Length - _realPosition);

            var left = realCount;
            var dstPos = offset;
            var pageOffset = (int) (_realPosition % _pageSize);
            
            for (long i = curPage; i < maxPage; i++)
            {
                var page = _buf[i];
                var toRead = Math.Min(left, _pageSize - pageOffset);
                
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
            EnsurePageLoadedByOffset(pos);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByOffset(long pos)
        {
            
            var page = pos / _pageSize;
            Debug.Assert(pos <= Length, $"offset {pos}, max {Length}");
            EnsurePageLoadedByIdx((int)page);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByIdx(int page)
        {
            if (page >= _totalPages)
            {
                return;
            }
            
            if (_buf[page] == null)
            {
                var offset = page * _pageSize;
                _underlying.Seek(offset, SeekOrigin.Begin);
                var spanSize = _pageSize;
                var max = offset + _pageSize;
                if (max > Length)
                {
                    spanSize =(int)( Length - offset);
                    Debug.Assert(spanSize < _pageSize);
                }

                var pagebuf = new byte[_pageSize];
                _buf[page] = pagebuf;
                var res = _underlying.Read(pagebuf);
                Debug.Assert(res > 0);
            }
        }
    }
}