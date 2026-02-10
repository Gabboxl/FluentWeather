using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FluentWeather.Utils
{
    public static class Json
    {
        public static async Task<T> ToObjectAsync<T>(string value)
        {
            return await Task.Run(() => (T)JsonSerializer.Deserialize(value, typeof(T), FluentWeatherJsonContext.Default));
        }

        public static async Task<string> StringifyAsync<T>(T value)
        {
            return await Task.Run(() => JsonSerializer.Serialize(value, typeof(T), FluentWeatherJsonContext.Default));
        }
    }
}
