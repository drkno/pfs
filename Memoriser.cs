using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pfs;

namespace pfs
{
    public class Memoriser
    {
        private TimeSpan _maxCacheAge;
        private IDictionary<long, CacheItem> _resultCache;
        public Memoriser(Configuration config)
        {
            _maxCacheAge = TimeSpan.FromMilliseconds(config.CacheAge);
            _resultCache = new Dictionary<long, CacheItem>();
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

            if (cached.Value is Exception)
            {
                throw cached.Value as Exception;
            }

            return (T) cached.Value;
        }

        public Q Memorise<T, Q>(Func<T, Q> callback, T arg)
        {
            var hash = arg.GetHashCode();
            var cacheResult = GetCachedResult<Q>(hash);
            if (object.Equals(cacheResult, default(Q)))
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
            var hash = arg.GetHashCode();
            var cacheResult = GetCachedResult<Q>(hash);
            if (object.Equals(cacheResult, default(Q)))
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
            var hash = arg1.GetHashCode() + 31 * arg2.GetHashCode();
            var cacheResult = GetCachedResult<Q>(hash);
            if (object.Equals(cacheResult, default(Q)))
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
            var hash = arg1.GetHashCode() + 31 * arg2.GetHashCode();
            var cacheResult = GetCachedResult<Q>(hash);
            if (object.Equals(cacheResult, default(Q)))
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
            var hash = arg1.GetHashCode() + 31 * arg2.GetHashCode() + 37 * arg3.GetHashCode();
            var cacheResult = GetCachedResult<Q>(hash);
            if (object.Equals(cacheResult, default(Q)))
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