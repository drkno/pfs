using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Pfs;

namespace pfs
{
    public class Memoriser
    {
        private readonly TimeSpan _maxCacheAge;
        private readonly IDictionary<long, CacheItem> _resultCache;
        public Memoriser(Configuration config)
        {
            _maxCacheAge = TimeSpan.FromMilliseconds(config.CacheAge);
            _resultCache = new Dictionary<long, CacheItem>();
        }

        private static long GenerateHashCode(params object[] input)
        {
            return input.Aggregate<object, long>(27, (current, i) => 13 * current + i.GetHashCode());
        }

        private T GetCachedResult<T>(long key)
        {
            if (!this._resultCache.ContainsKey(key))
            {
                return default(T);
            }

            var cached = _resultCache[key];
            if (DateTimeOffset.Now - cached.AddedAt >= _maxCacheAge)
            {
                _resultCache.Remove(key);
                return default(T);
            }

            if (cached.Value is Exception exception)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return (T) cached.Value;
        }

        public Q Memorise<T, Q>(Func<T, Q> callback, T arg)
        {
            var hash = GenerateHashCode(callback, arg);
            var cacheResult = GetCachedResult<Q>(hash);
            if (!object.Equals(cacheResult, default(Q)))
            {
                return cacheResult;
            }
            try
            {
                var result = callback(arg);
                _resultCache[hash] = new CacheItem(result);
                return result;
            }
            catch (Exception e)
            {
                _resultCache[hash] = new CacheItem(e);
                throw;
            }
        }

        public async Task<Q> Memorise<T, Q>(Func<T, Task<Q>> callback, T arg)
        {
            var hash = GenerateHashCode(callback, arg);
            var cacheResult = GetCachedResult<Q>(hash);
            if (!object.Equals(cacheResult, default(Q)))
            {
                return cacheResult;
            }
            try
            {
                var result = await callback(arg);
                _resultCache[hash] = new CacheItem(result);
                return result;
            }
            catch (Exception e)
            {
                _resultCache[hash] = new CacheItem(e);
                throw;
            }
        }

        public Q Memorise<P, T, Q>(Func<P, T, Q> callback, P arg1, T arg2)
        {
            var hash = GenerateHashCode(callback, arg1, arg2);
            var cacheResult = GetCachedResult<Q>(hash);
            if (!object.Equals(cacheResult, default(Q)))
            {
                return cacheResult;
            }
            try
            {
                var result = callback(arg1, arg2);
                _resultCache[hash] = new CacheItem(result);
                return result;
            }
            catch (Exception e)
            {
                _resultCache[hash] = new CacheItem(e);
                throw;
            }
        }

        public async Task<Q> Memorise<P, T, Q>(Func<P, T, Task<Q>> callback, P arg1, T arg2)
        {
            var hash = GenerateHashCode(callback, arg1, arg2);
            var cacheResult = GetCachedResult<Q>(hash);
            if (!object.Equals(cacheResult, default(Q)))
            {
                return cacheResult;
            }
            try
            {
                var result = await callback(arg1, arg2);
                _resultCache[hash] = new CacheItem(result);
                return result;
            }
            catch (Exception e)
            {
                _resultCache[hash] = new CacheItem(e);
                throw;
            }
        }

        public async Task<Q> Memorise<P, T, R, Q>(Func<P, T, R, Task<Q>> callback, P arg1, T arg2, R arg3)
        {
            var hash = GenerateHashCode(callback, arg1, arg2, arg3);
            var cacheResult = GetCachedResult<Q>(hash);
            if (!object.Equals(cacheResult, default(Q)))
            {
                return cacheResult;
            }
            try
            {
                var result = await callback(arg1, arg2, arg3);
                _resultCache[hash] = new CacheItem(result);
                return result;
            }
            catch (Exception e)
            {
                _resultCache[hash] = new CacheItem(e);
                throw;
            }
        }

        private class CacheItem
        {
            public CacheItem(object value)
            {
                Value = value;
                AddedAt = DateTime.Now;
            }
            public object Value { get; }
            
            public DateTime AddedAt { get; }
        }
    }
}