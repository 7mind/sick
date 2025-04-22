#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SickSharp.Format
{
    public sealed class PageCachedFile : IDisposable
    {
        public int PageSize { get; }
        public int TotalPages { get; }
        public long Length { get; }
        
        private readonly byte[]?[] _buf;
        private readonly int[] _status; // 0 -> missing, 1 -> loading, 2 -> loaded
        
        private FileStream? _underlying;
        private readonly object _lock = new();
        
        private int _processedPages;
        private volatile bool _disposed;
        
        private readonly TaskCompletionSource<bool>[] _latches;  
        
        private void ThrowIfDisposed() {
            if (_disposed) throw new ObjectDisposedException(nameof(PageCachedFile));
        }
        
        public PageCachedFile(string path, int pageSize)
        {
            Debug.Assert(pageSize > 0);
            
            var info = new FileInfo(path);
            Length = info.Length;
            
            PageSize = pageSize;
            var fullSize = (Length + PageSize - 1) / PageSize;
            Debug.Assert(fullSize <= Int32.MaxValue);
            TotalPages = (int)(fullSize);
            _processedPages = 0;
            
            _buf = new byte[TotalPages][];
            _status = new int[TotalPages];
            _latches = new TaskCompletionSource<bool>[TotalPages];

            if (TotalPages > 0)
            {
                for (int i = 0; i < TotalPages; i++)
                {
                    _status[i] = 0;
                    // I have no idea if TaskCreationOptions.RunContinuationsAsynchronously is necessary 
                    _latches[i] = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    Debug.Assert(_buf[i] == null);
                }
            
                _underlying = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);    
            }
            else
            {
                _underlying = null;
            }
            
        }
        
        public double CacheSaturation()
        {
            var currentlyAllocated = Volatile.Read(ref _processedPages);
            return TotalPages == 0 ? 1.0 : ((double) currentlyAllocated) / TotalPages;
        }

        public byte[]? GetPage(int page)
        {
            ThrowIfDisposed();
            
            if (page < 0 || page >= TotalPages)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }
            
            // if it fails, we will throw, next call will reattempt loading
            EnsurePageLoadedByIdx(page);
            
            Debug.Assert(Volatile.Read(ref _status[page]) == 2 && Volatile.Read(ref _buf[page]) != null);
            
            return Volatile.Read(ref _buf[page]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByIdx(int page)
        {
            if (page < 0 || page >= TotalPages) return;
            
            if (Volatile.Read(ref _status[page]) == 2) return;

            var currentStatus = Interlocked.CompareExchange(ref _status[page], 1, 0);
            if (currentStatus == 0)
            {
                // page reads will be serialized, that's fine, there is no easy fix because Files are thread-unsafe
                // an async version with ReadAsync is possible though
                lock (_lock) 
                {
                    if (Interlocked.CompareExchange(ref _status[page], 1, 1) == 1)
                    {
                        try
                        {
                            ThrowIfDisposed();
                            Debug.Assert(_underlying != null);
                            var offset = page * PageSize;
                            var newPage = new byte[PageSize];
                            
                            _underlying!.Seek(offset, SeekOrigin.Begin);
                            var read = _underlying!.Read(newPage, 0, newPage.Length);
                            Debug.Assert(read > 0);

                            if (read < PageSize)
                            {
                                newPage = newPage[..read];
                            }
                            
                            Volatile.Write(ref _buf[page], newPage);
                            Interlocked.Increment(ref _processedPages);
                            Volatile.Write(ref _status[page], 2);
                            
                            if (Volatile.Read(ref _processedPages) == TotalPages)
                            {
                                _underlying.Close();
                                _underlying.Dispose();
                                _underlying = null;
                            }
                            
                            _latches[page].TrySetResult(true);
                        }
                        catch (Exception t)
                        {
                            // we will re-attempt loading
                            Volatile.Write(ref _status[page], 0);

                            // we will create new latch for threads which will wait for further attempts
                            var existingLatch = Interlocked.Exchange(ref _latches[page], new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
                            
                            // here we fail existing waiting threads
                            existingLatch.TrySetException(t);
                            
                            throw;
                        }
                    }
                }
            }
            else if (currentStatus == 1)
            {
                _latches[page].Task.Wait();
            }
        }


        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            lock (_lock)
            {
                _disposed = true;

                for (int i = 0; i < TotalPages; i++)
                {
                    Interlocked.Exchange(ref _status[i], 0);
                    Interlocked.Exchange(ref _buf[i], null);
                    _latches[i].TrySetCanceled();
                }
                
                _underlying?.Dispose();
            }
        }
    }
}