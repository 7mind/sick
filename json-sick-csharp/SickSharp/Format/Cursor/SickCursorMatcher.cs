#nullable enable
using System.Numerics;
using SickSharp.Format.Tables;

namespace SickSharp
{
    public abstract class SickCursorMatcher<T> where T : class
    {
        public virtual T? OnNull()
        {
            return null;
        }

        public virtual T? OnBool(bool value)
        {
            return null;
        }

        public virtual T? OnByte(sbyte value)
        {
            return null;
        }

        public virtual T? OnShort(short value)
        {
            return null;
        }

        public virtual T? OnInt(int value)
        {
            return null;
        }

        public virtual T? OnLong(long value)
        {
            return null;
        }

        public virtual T? OnBigInt(BigInteger value)
        {
            return null;
        }

        public virtual T? OnFloat(float value)
        {
            return null;
        }

        public virtual T? OnDouble(double value)
        {
            return null;
        }

        public virtual T? OnBigDecimal(BigDecimal value)
        {
            return null;
        }

        public virtual T? OnString(string value)
        {
            return null;
        }

        public virtual T? OnArray(SickCursor.Array value)
        {
            return null;
        }

        public virtual T? OnObj(SickCursor.Object value)
        {
            return null;
        }

        public virtual T? OnRoot(SickRoot value)
        {
            return null;
        }
    }
}