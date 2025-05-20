using System;
using System.Collections.Generic;

namespace SickSharp
{
    public abstract class SickBaseReader
    {
        /**
         * Query value at specified path.
         * <param name="query">
         *      Dot-separated path with jsonpath-styled array indexing.
         *      Examples: `my.path`; `my.array[3].path`; `my.array.3.path`; `my.array.[3].path`
         * </param>
         */
        public virtual SickJson Query(string query)
        {
            throw new KeyNotFoundException($"Can not query `{query}` from <{GetType().Name}>.");
        }

        /**
         * Try to query value at specified path.
         * <param name="query">
         *      Dot-separated path with jsonpath-styled array indexing.
         *      Examples: `my.path`; `my.array[3].path`; `my.array.3.path`; `my.array.[3].path`
         * </param>
         * <param name="value">Output JSON value.</param>
         */
        public bool TryQuery(string query, out SickJson value)
        {
            try
            {
                value = Query(query);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Query value with specified type at specified path.
         * <param name="query">
         *      Dot-separated path with jsonpath-styled array indexing. 
         *      Examples: `my.path`; `my.array[3].path`; `my.array.3.path`; `my.array.[3].path`
         * </param>
         */
        public T Query<T>(string query) where T : SickJson
        {
            var result = Query(query);
            if (result is T t) return t;
            throw new KeyNotFoundException($"Query `{query}` returned <{result.GetType().Name}> while expecting <{typeof(T).Name}>.");
        }

        /**
         * Try to query value with specified type at specified path.
         * <param name="query">
         *      Dot-separated path with jsonpath-styled array indexing.
         *      Examples: `my.path`; `my.array[3].path`; `my.array.3.path`; `my.array.[3].path`
         * </param>
         * <param name="value">Output JSON value.</param>
         */
        public bool TryQuery<T>(string query, out T value) where T : SickJson
        {
            try
            {
                value = Query<T>(query);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Read value at specified path.
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public virtual SickJson Read(ReadOnlySpan<string> path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path.ToArray())}` from <{GetType().Name}>.");
        }

        /**
         * Read value with specified type at specified path.
         * <param name="value">Output value.</param>
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public bool TryRead(out SickJson value, ReadOnlySpan<string> path)
        {
            try
            {
                value = Read(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Read value with specified type at specified path.
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public T Read<T>(ReadOnlySpan<string> path) where T : SickJson
        {
            var result = Read(path);
            if (result is T t) return t;
            throw new KeyNotFoundException($"Field `{string.Join(".", path.ToArray())}` returned <{result.GetType().Name}> while expecting <{typeof(T).Name}>.");
        }

        /**
         * Try to read value with specified type at specified path.
         * <param name="value">Output value.</param>
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public bool TryRead<T>(out T value, ReadOnlySpan<string> path) where T : SickJson
        {
            try
            {
                value = Read<T>(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Read value at specified path.
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public virtual SickJson Read(params string[] path)
        {
            throw new KeyNotFoundException($"Can not read field `{string.Join(".", path)}` from <{GetType().Name}>.");
        }

        /**
         * Read value with specified type at specified path.
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public T Read<T>(params string[] path) where T : SickJson
        {
            var result = Read(path);
            if (result is T t) return t;
            throw new KeyNotFoundException($"Field `{string.Join(".", path)}` returned <{result.GetType().Name}> while expecting <{typeof(T).Name}>.");
        }

        /**
         * Try to read value at specified path.
         * <param name="value">Output value.</param>
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public bool TryRead(out SickJson value, params string[] path)
        {
            try
            {
                value = Read(path);
                return true;
            }
            catch (Exception)
            {
                value = null!;
                return false;
            }
        }

        /**
         * Try to read value with specified type at specified path.
         * <param name="value">Output value.</param>
         * <param name="path">
         *      Path as a segment string array.
         *      Examples: {my, path}; {my, array, [3], path}; {my, array, 3, path}; {my, array, [3], path}
         * </param>
         */
        public bool TryRead<T>(out T value, params string[] path) where T : SickJson
        {
            try
            {
                value = Read<T>(path);
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