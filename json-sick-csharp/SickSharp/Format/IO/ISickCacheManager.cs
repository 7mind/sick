#nullable enable
using System;
using System.Collections.Concurrent;

namespace SickSharp.IO
{
    public interface ISickCacheManager : IDisposable
    {
        PageCachedFile Acquire(string path, int pageSize, ISickProfiler profiler);
        void Return(PageCachedFile file);

        private static readonly Lazy<NoCacheManager> NoCacheInstance = new Lazy<NoCacheManager>(() => new NoCacheManager());
        public static ISickCacheManager NoCache => NoCacheInstance.Value;

        private static readonly Lazy<PerFileCacheManager> GlobalPerFileInstance = new Lazy<PerFileCacheManager>(() => new PerFileCacheManager());
        public static ISickCacheManager GlobalPerFile => GlobalPerFileInstance.Value;

        public sealed class NoCacheManager : ISickCacheManager
        {
            public PageCachedFile Acquire(string path, int pageSize, ISickProfiler profiler)
            {
                return new PageCachedFile(path, pageSize, profiler);
            }

            public void Return(PageCachedFile file)
            {
                file.Dispose();
            }

            public void Dispose()
            {
            }
        }

        public class PerFileCacheManager : ISickCacheManager
        {
            private readonly ConcurrentDictionary<string, PageCachedFile> _cache = new();

            public void Return(PageCachedFile file)
            {
            }

            public PageCachedFile Acquire(string path, int pageSize, ISickProfiler profiler)
            {
                return _cache.GetOrAdd($"{pageSize}:{path}", _ => new PageCachedFile(path, pageSize, profiler));
            }

            public void Dispose()
            {
                foreach (var (_, value) in _cache) value.Dispose();
                _cache.Clear();
            }
        }
    }
}