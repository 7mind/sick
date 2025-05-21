using System;
using System.Collections.Generic;

namespace SickSharp
{
    public abstract class SickObjectReader : SickBaseReader
    {
        /**
         * Read key of Sick object at the specified index.
         * <param name="index">Index of the key.</param>
         * <returns>Field name with cursor to the queried reference. Cursor value evaluated lazily and requires active SickReader.</returns>
         */
        public virtual KeyValuePair<string, SickCursor> ReadKey(int index)
        {
            throw new KeyNotFoundException($"Can not read indexed key field [{index}] from <{GetType().Name}>.");
        }

        /**
         * Read key of Sick object with specified type at the specified index.
         * <param name="index">Index of the key.</param>
         * <returns>Field name with cursor to the queried reference. Cursor value evaluated lazily and requires active SickReader.</returns>
         */
        public KeyValuePair<string, T> ReadKey<T>(int index) where T : SickCursor
        {
            var (key, value) = ReadKey(index);
            if (value is T t) return new KeyValuePair<string, T>(key, t);
            throw new KeyNotFoundException($"Field [{index}] returned <{value.GetType().Name}> while expecting <{typeof(T).Name}>.");
        }

        /**
         * Try to read key of Sick object at the specified index.
         * <param name="value">Field name with cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</param>
         * <param name="index">Index of the key.</param>
         */
        public bool TryReadKey(out KeyValuePair<string, SickCursor> value, int index)
        {
            try
            {
                value = ReadKey(index);
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }

        /**
         * Try to read key of Sick object with specified type at the specified index.
         * <param name="value">Field name with cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</param>
         * <param name="index">Index of the key.</param>
         */
        public bool TryReadKey<T>(out KeyValuePair<string, T> value, int index) where T : SickCursor
        {
            try
            {
                value = ReadKey<T>(index);
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }

        /**
         * Read value at specified index.
         * <param name="index">Index of the object field or array element.</param>
         * <returns>Cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</returns>
         */
        public virtual SickCursor ReadIndex(int index)
        {
            throw new KeyNotFoundException($"Can not read indexed field [{index}] from <{GetType().Name}>.");
        }

        /**
         * Read value with specified type at specified index.
         * <param name="index">Index of the object field or array element.</param>
         * <returns>Cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</returns>
         */
        public T ReadIndex<T>(int index) where T : SickCursor
        {
            var result = ReadIndex(index);
            if (result is T t) return t;
            throw new KeyNotFoundException($"Field [{index}] returned <{result.GetType().Name}> while expecting <{typeof(T).Name}>.");
        }

        /**
         * Try to read value at specified index.
         * <param name="value">Cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</param>
         * <param name="index">Index of the object field or array element.</param>
         */
        public bool TryReadIndex(out SickCursor value, int index)
        {
            try
            {
                value = ReadIndex(index);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Try to read value with specified type at specified index.
         * <param name="value">Cursor to the indexed element. Cursor value evaluated lazily and requires active SickReader.</param>
         * <param name="index">Index of the object field or array element.</param>
         */
        public bool TryReadIndex<T>(out T value, int index) where T : SickCursor
        {
            try
            {
                value = ReadIndex<T>(index);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }
    }
}