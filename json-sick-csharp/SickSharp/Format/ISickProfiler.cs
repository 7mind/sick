#nullable enable
using System;

namespace SickSharp
{
    public interface ISickCallProfiler : IDisposable
    {
        T OnReturn<T>(T value);
        void OnCheckpoint(string clue, params object[] args);
    }

    public interface ISickProfiler
    {
        public ISickCallProfiler OnInvoke(string id, params object[] args);
        public void OnStats(string id, long value);

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

        public void OnCheckpoint(string clue, params object[] args)
        {
        }
    }

    public class NoopSickProfiler : ISickProfiler
    {
        private static readonly ISickCallProfiler Noop = new NoopSickCallProfiler();

        public ISickCallProfiler OnInvoke(string id, params object[] args)
        {
            return Noop;
        }

        public void OnStats(string id, long value)
        {
        }
    }
}