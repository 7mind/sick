#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SickSharp.Primitives;

namespace SickSharp.IO
{
    public enum CachePageStatus : int
    {
        Missing = 0,
        Loading = 1,
        Loaded = 2,
        Dead = -1
    }

    public sealed class PageCachedFile : IDisposable
    {
        public int PageSize { get; }
        public int TotalPages { get; }
        public long Length { get; }

        private readonly byte[]?[] _buf;
        private readonly int[] _status;

        private FileStream? _underlying;
        private readonly object _lock = new();

        private int _processedPages;
        private volatile bool _disposed;

        private readonly TaskCompletionSource<bool>[] _pageLoadedLatches;
        private readonly ISickProfiler _profiler;

        public CachePageStatus GetPageStatus(int page)
        {
            var id = Volatile.Read(ref _status[page]);
            return (CachePageStatus)id;
        }

        public PageCachedFile(string path, int pageSize, ISickProfiler profiler)
        {
            Debug.Assert(pageSize > 0);
            _profiler = profiler;

            Length = new FileInfo(path).Length;
            PageSize = pageSize;

            var fullSize = (Length + PageSize - 1) / PageSize;
            Debug.Assert(fullSize <= int.MaxValue);
            TotalPages = (int)fullSize;

            _processedPages = 0;
            _buf = new byte[TotalPages][];
            _status = new int[TotalPages];
            _pageLoadedLatches = new TaskCompletionSource<bool>[TotalPages];

            if (TotalPages > 0)
            {
                for (int i = 0; i < TotalPages; i++)
                {
                    _status[i] = (int)CachePageStatus.Missing;
                    // I have no idea if TaskCreationOptions.RunContinuationsAsynchronously is necessary 
                    _pageLoadedLatches[i] =
                        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
            return TotalPages == 0 ? 1.0 : ((double)currentlyAllocated) / TotalPages;
        }

        public byte[]? GetPage(int page)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"Cache is being disposed, refusing to provide page {page}");
            }

            if (page < 0 || page >= TotalPages)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            var status = Volatile.Read(ref _status[page]);

            if (status == (int)CachePageStatus.Loaded)
            {
                var result = Volatile.Read(ref _buf[page]);
                Debug.Assert(result != null);
                return result;
            }

            EnsurePageLoadedByIdx(page);

            status = Volatile.Read(ref _status[page]);

            if (status == (int)CachePageStatus.Loaded)
            {
                return Volatile.Read(ref _buf[page])!;
            }

            return status switch
            {
                (int)CachePageStatus.Dead => throw new ObjectDisposedException(
                    $"Page {page} is marked as dead, either cache is being disposed, or there was a loading exception"
                ),
                _ => throw new InvalidOperationException(
                    $"Unexpected page state after loading: {status}"
                )
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePageLoadedByIdx(int page)
        {
            if (page < 0 || page >= TotalPages) return;

            var currentStatus = Interlocked.CompareExchange(
                ref _status[page],
                (int)CachePageStatus.Loading,
                (int)CachePageStatus.Missing
            );

            if (currentStatus == (int)CachePageStatus.Loaded)
            {
                return;
            }

            switch (currentStatus)
            {
                case (int)CachePageStatus.Missing:
                    try
                    {
                        LoadPageSynchronously(page);
                        _pageLoadedLatches[page].TrySetResult(true);
                    }
                    catch (Exception t)
                    {
                        Interlocked.Exchange(ref _status[page], (int)CachePageStatus.Dead);
                        // here we fail existing waiting threads
                        _pageLoadedLatches[page].TrySetException(t);
                        throw;
                    }

                    break;

                case (int)CachePageStatus.Loading:
                    _pageLoadedLatches[page].Task.Wait();
                    break;

                case (int)CachePageStatus.Dead:
                    throw new ObjectDisposedException(
                        $"Page {page} is marked as dead, either cache is being disposed, or there was a loading exception");
            }
        }

        public async Task<byte[]?> GetPageAsync(int page)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"Cache is being disposed, refusing to provide page {page}");
            }

            if (page < 0 || page >= TotalPages)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            var status = Volatile.Read(ref _status[page]);

            if (status == (int)CachePageStatus.Loaded)
            {
                var result = Volatile.Read(ref _buf[page]);
                Debug.Assert(result != null);
                return result;
            }

            await EnsurePageLoadedByIdxAsync(page).ConfigureAwait(false);
            status = Volatile.Read(ref _status[page]);

            return status switch
            {
                (int)CachePageStatus.Loaded =>
                    Volatile.Read(ref _buf[page])!,
                (int)CachePageStatus.Dead =>
                    throw new ObjectDisposedException(
                        $"Page {page} is marked as dead, either cache is being disposed, or there was a loading exception"),
                _ =>
                    throw new InvalidOperationException($"Unexpected page state after loading: {status}")
            };
        }

        private async Task EnsurePageLoadedByIdxAsync(int page)
        {
            if (page < 0 || page >= TotalPages) return;

            var currentStatus = Interlocked.CompareExchange(
                ref _status[page],
                (int)CachePageStatus.Loading,
                (int)CachePageStatus.Missing
            );

            switch ((CachePageStatus)currentStatus)
            {
                case CachePageStatus.Loaded:
                    return;

                case CachePageStatus.Missing:
                    try
                    {
                        // Offload synchronous file IO to threadpool
                        await Task.Run(() => LoadPageSynchronously(page)).ConfigureAwait(false);
                        _pageLoadedLatches[page].TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Exchange(ref _status[page], (int)CachePageStatus.Dead);
                        _pageLoadedLatches[page].TrySetException(ex);
                        throw;
                    }

                    break;

                case CachePageStatus.Loading:
                    await _pageLoadedLatches[page].Task.ConfigureAwait(false);
                    break;

                case CachePageStatus.Dead:
                    throw new ObjectDisposedException(
                        $"Page {page} is marked as dead, either cache is being disposed, or there was a loading exception");
            }
        }

        private void LoadPageSynchronously(int page)
        {
            Debug.Assert(_underlying != null);

#if SICK_PROFILE_READER
            using (var cp = _profiler.OnInvoke("LoadPageSynchronously", _info, page))
#endif
            {
                var offset = page * PageSize;
                var newPage = new byte[PageSize];

                int read;
                lock (_lock)
                {
                    if (_underlying == null) throw new ObjectDisposedException($"Cache is being disposed, page {page} won't be loaded");

                    _underlying.Seek(offset, SeekOrigin.Begin);
                    read = _underlying.ReadUpTo(newPage, 0, newPage.Length);
                }

                if (read < PageSize)
                {
                    newPage = newPage[..read];
                }

                var pageBeforeUpdate = Interlocked.CompareExchange(ref _buf[page], newPage, null);

                if (pageBeforeUpdate == null)
                {
                    Interlocked.CompareExchange(
                        ref _status[page],
                        (int)CachePageStatus.Loaded,
                        (int)CachePageStatus.Loading
                    );

                    if (Interlocked.Increment(ref _processedPages) == TotalPages)
                    {
                        lock (_lock)
                        {
                            _underlying?.Dispose();
                            _underlying = null;
                        }
                    }
                }

                _profiler.OnStats("CacheSaturation", (long)(CacheSaturation() * 100));
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
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                for (int i = 0; i < TotalPages; i++)
                {
                    Interlocked.Exchange(ref _status[i], (int)CachePageStatus.Dead);
                    // we don't clean _buf in order to prevent NPEs in user threads
                    _pageLoadedLatches[i].TrySetCanceled();
                }

                _underlying?.Dispose();
            }
        }
    }
}