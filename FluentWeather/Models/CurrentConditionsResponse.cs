using System.Text.Json.Serialization;

namespace FluentWeather.Models
{
    /// <summary>
    /// Small response with only the current conditions and the place name.
    /// Used to keep the tray icons up to date for every saved location.
    /// </summary>
    public class CurrentConditionsResponse
    {
        [JsonPropertyName("v3-wx-observations-current")]
        public V3WxObservationsCurrent? v3wxobservationscurrent { get; set; }

        [JsonPropertyName("v3-location-point")]
        public V3LocationPoint? v3locationpoint { get; set; }
    }
}
