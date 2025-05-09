#nullable enable
using System;

namespace SickSharp.Format
{

    public interface ISickCallProfiler : IDisposable
    {
        T OnReturn<T>(T value);
    }
    public interface ISickProfiler
    {

        public ISickCallProfiler OnInvoke(string id, params object[] args);
        
        private static readonly ISickProfiler DummyInstance = new NoopSickProfiler();
        
        public static ISickProfiler Noop()
        {
            return DummyInstance;
        }
    }

    public class NoopSickCallProfiler : ISickCallProfiler
    {
        public void Dispose()
        {
            
        }

        public T OnReturn<T>(T value)
        {
            return value;
        }
    }
    public class NoopSickProfiler : ISickProfiler
    {
        private static readonly ISickCallProfiler Noop = new NoopSickCallProfiler();
        
        public ISickCallProfiler OnInvoke(string id, params object[] args)
        {
            return Noop;
        }
    }
}