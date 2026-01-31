using System;
using System.Collections.Concurrent;

namespace FluentWeather.Core.Helpers
{
    public static class Singleton<T>
        where T : new()
    {
        private static readonly ConcurrentDictionary<Type, T> Instances = new ConcurrentDictionary<Type, T>();

        public static T Instance
        {
            get
            {
                return Instances.GetOrAdd(typeof(T), (t) => new T());
            }
        }
    }
}
