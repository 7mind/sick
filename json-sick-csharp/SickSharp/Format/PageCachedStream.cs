using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SickSharp.Format
{
    public sealed class PageCachedStream : Stream
    {
        private int _pageSize;
        private byte[] _buf;
        private bool[] _loadedPagesIndex;
        private long _realPosition;
        private FileStream _underlying;
        
        public PageCachedStream(string path, int pageSize)
        {
            var info = new FileInfo(path);
            Length = info.Length;
            _buf = new byte[Length];
            _realPosition = 0;
            _pageSize = pageSize;
            var totalPages = (Length + _pageSize - 1) / _pageSize;
            _loadedPagesIndex = new bool[totalPages];
            for (int i = 0; i < totalPages; i++)
            {
                _loadedPagesIndex[i] = false;
            }
            
            _underlying = File.Open(path, FileMode.Open);
        }

        ~PageCachedStream()
        {
            Dispose(false);
        }
        
        protected sealed override void Dispose(bool disposing)
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
            var curPage = (_realPosition + _pageSize - 1) / _pageSize;
            var maxPage = (_realPosition + count + _pageSize - 1) / _pageSize;           
            
            for (long i = curPage; i < maxPage; i++)
            {
                EnsurePageLoadedByIdx((int)i);
            }

            
            var dest = buffer.AsSpan(offset, count);
            var src = _buf.AsSpan((int)_realPosition, count);
            src.CopyTo(dest);
            Position += count;
            return count;
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
            Debug.Assert(page < _loadedPagesIndex.Length, $"page {page}, max={_loadedPagesIndex.Length}, size={_pageSize}, len={Length}");
            
            if (!_loadedPagesIndex[page])
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
                var span = _buf.AsSpan((int)offset, spanSize);
                var res = _underlying.Read(span);
                Debug.Assert(res > 0);
                _loadedPagesIndex[page] = true;
            }
        }
    }
}