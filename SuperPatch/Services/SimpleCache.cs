using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SuperPatch.Core;

namespace SuperPatch.Services
{
  public class Cache<T>
  {
    private Dictionary<string, T> cache = new Dictionary<string, T>();

    public async Task<T> GetOrSetAsync(string Key, Func<Task<T>> getter)
    {
      if (cache.ContainsKey(Key)) return cache[Key];

      PurgeOldObject();

      var c = await getter();
      cache[Key] = c;
      return c;
    }

    private void PurgeOldObject()
    {
      if (cache.Count() > 10) cache.Clear();
    }
  }

  public class SimpleCache
  {
    private Dictionary<Type, object> caches = new Dictionary<Type, object>();

    public Cache<T> ForType<T>()
    {
      Cache<T> cache = null;

      if (caches.ContainsKey(typeof(T)))
        cache = (Cache<T>)caches[typeof(T)];

      if (cache != null) return (Cache<T>)cache;

      cache = new Cache<T>();
      caches[typeof(T)] = cache;
      return (Cache<T>)cache;
    }
  }
}
