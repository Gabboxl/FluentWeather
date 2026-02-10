using CommunityToolkit.Common;
using FluentWeather.Converters;
using FluentWeather.Models;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(RootV3Response))]
[JsonSerializable(typeof(SearchLocationResponse))]
[JsonSerializable(typeof(bool?))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = new[] { typeof(CustomDateTimeOffsetConverter) }
)]
public partial class FluentWeatherJsonContext : JsonSerializerContext
{
}
