using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SuperPatch.Core.Server.Infrastructure.Context
{
  public static class DeepCopyExtensions
  {
    private static readonly Dictionary<Type, IPropsCache> cacheList = [];

    public static IPropsCache GetCacheFor<T>()
    {
      var type = typeof(T);

      if (!cacheList.TryGetValue(type, out var cached))
      {
        cached = new PropsCache<T>();
        cacheList[type] = cached;
      }
      return cached;
    }

    public static void DeepCopy<T, X>(this T dst, X src)
    {
      var propDst = GetCacheFor<T>().GetProps();
      var propSrc = GetCacheFor<X>().GetProps();

      foreach (var prop in propDst)
      {
        var p = propSrc.FirstOrDefault(x => x.NameUpper == prop.NameUpper);
        if (p != null)
          prop.SetValue(dst, p.GetValue(src));
      }
    }

    public interface IPropsCache
    {
      List<LoadFromProps> GetProps();
    }

    public class PropsCache<T> : IPropsCache
    {
      internal List<LoadFromProps> CachedProps { get; set; } = default!;
      internal Dictionary<string, List<LoadFromProps>> cache_propsFiltered = [];

      public List<LoadFromProps> GetProps()
      {
        if (CachedProps == null)
        {
          var currentProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
          if (typeof(T).BaseType != null)
          {
            currentProps = [.. currentProps.Union(typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))];
          }

          CachedProps = [.. currentProps
                    .Select(x => new LoadFromProps() {
                      MemberInfo = x,
                      NameUpper = x.Name.ToUpper(),
                      TypeConverter = TypeDescriptor.GetConverter(x.PropertyType),
                      ColumnName = x.GetCustomAttribute<ColumnAttribute>()?.Name,
                      NotMapped = x.GetCustomAttribute<NotMappedAttribute>() != null
                    })
                    .Where(x => !x.NotMapped)];

          CachedProps.ForEach(x =>
          {
            if (x.TypeConverter is not CustomTypeLoaderDescriptor)
            {
              x.TypeConverter = null;
            }
          });
        }
        return CachedProps;
      }

      internal List<LoadFromProps> GetPropsFiltered(string fieldName)
      {
        if (cache_propsFiltered.TryGetValue(fieldName, out var value))
          return value;

        var props = GetProps();

        var propsFiltered =
          props.Where(x =>
          {
            if (x.NameUpper == fieldName || x.NameUpper == "_" + fieldName)
              return true;
            else
              return false;
          }).ToList();
        cache_propsFiltered[fieldName] = propsFiltered;

        return propsFiltered;
      }
    }

    public class LoadFromProps
    {
      public required PropertyInfo MemberInfo { get; set; }
      public required string NameUpper { get; set; }
      public required TypeConverter? TypeConverter { get; set; }
      public required string? ColumnName { get; set; }
      public bool NotMapped { get; internal set; }

      public object? GetValue<T>(T obj)
      {
        return MemberInfo.GetValue(obj);
      }

      public void SetValue<T>(T dst, object? v)
      {
        MemberInfo.SetValue(dst, v);
      }
    }

    public abstract class CustomTypeLoaderDescriptor : TypeConverter
    {
      protected CustomTypeLoaderDescriptor()
      {
      }
    }
  }
}
