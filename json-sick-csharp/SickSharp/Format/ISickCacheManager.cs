#nullable enable
using System.Collections.Concurrent;

namespace SickSharp.Format
{
    public interface ISickCacheManager
    {
        PageCachedFile Provide(string path, int pageSize);

        private static readonly ISickCacheManager DummyInstance = new DummySickCacheManager();
        
        public static ISickCacheManager Dummy()
        {
            return DummyInstance;
        }
        
        private static readonly ISickCacheManager GlobalPerFileInstance = new PerFileSickCacheManager();
        public static ISickCacheManager GlobalPerFile()
        {
            return GlobalPerFileInstance;
        }
    }
    
    public class DummySickCacheManager : ISickCacheManager
    {
        public PageCachedFile Provide(string path, int pageSize)
        {
            return new PageCachedFile(path, pageSize);
        }
    }

    public class PerFileSickCacheManager : ISickCacheManager
    {
        private ConcurrentDictionary<string, PageCachedFile> _cache = new();

        public PageCachedFile Provide(string path, int pageSize)
        {
            return _cache.GetOrAdd($"{pageSize}:{path}", _ => new PageCachedFile(path, pageSize));
        }
    }
}