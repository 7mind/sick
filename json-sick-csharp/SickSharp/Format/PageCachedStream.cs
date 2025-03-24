using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SickSharp.Format
{
    public sealed class PageCachedStream : Stream
    {
        private int PageSize;
        private byte[] Buf;
        private bool[] LoadedPagesIndex;
        private long RealPosition = -1;
        private FileStream Underlying;
        
        public PageCachedStream(string path, int pageSize)
        {
            var info = new FileInfo(path);
            Length = info.Length;
            Buf = new byte[Length];
            RealPosition = 0;
            PageSize = pageSize;
            var totalPages = (Length + PageSize - 1) / PageSize;
            LoadedPagesIndex = new bool[totalPages];
            for (int i = 0; i < totalPages; i++)
            {
                LoadedPagesIndex[i] = false;
            }
            
            Underlying = File.Open(path, FileMode.Open);
        }

        ~PageCachedStream()
        {
            Dispose(false);
        }
        
        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Underlying?.Dispose();
            }

            base.Dispose(disposing);
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (Underlying != null) await Underlying.DisposeAsync();
        }

        public sealed override async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var curPage = (RealPosition + PageSize - 1) / PageSize;
            var maxPage = (RealPosition + count + PageSize - 1) / PageSize;           
            
            for (long i = curPage; i < maxPage; i++)
            {
                EnsurePageLoadedByIdx((int)i);
            }

            
            var dest = buffer.AsSpan(offset, count);
            var src = Buf.AsSpan((int)RealPosition, count);
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
                    pos = RealPosition + offset;
                    break;
                case SeekOrigin.End:
                    pos = Length - offset;
                    break;
            }
            Debug.Assert(pos <= Length, $"pos {pos} len={Length}");
            EnsurePageLoadedByOffset(pos);
            RealPosition = pos;
            return RealPosition;
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
            get => RealPosition;
            set => Seek(value, SeekOrigin.Begin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByOffset(long pos)
        {
            
            var page = pos / PageSize;
            Debug.Assert(pos <= Length, $"offset {pos}, max {Length}");
            EnsurePageLoadedByIdx((int)page);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByIdx(int page)
        {
            Debug.Assert(page < LoadedPagesIndex.Length, $"page {page}, max={LoadedPagesIndex.Length}, size={PageSize}, len={Length}");
            
            if (!LoadedPagesIndex[page])
            {
                var offset = page * PageSize;
                Underlying.Seek(offset, SeekOrigin.Begin);
                var spanSize = PageSize;
                var max = offset + PageSize;
                if (max > Length)
                {
                    spanSize =(int)( Length - offset);
                    Debug.Assert(spanSize < PageSize);
                }
                var span = Buf.AsSpan((int)offset, spanSize);
                var res = Underlying.Read(span);
                Debug.Assert(res > 0);
                LoadedPagesIndex[page] = true;
            }
        }
    }
}