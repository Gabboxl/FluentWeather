using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FluentWeather.Models
{
    /// <summary>
    /// Small response with the current conditions, the place name and the moon times of the next days.
    /// Used to keep the tray icons up to date for every saved location.
    /// </summary>
    public class CurrentConditionsResponse
    {
        [JsonPropertyName("v3-wx-observations-current")]
        public V3WxObservationsCurrent? v3wxobservationscurrent { get; set; }

        [JsonPropertyName("v3-location-point")]
        public V3LocationPoint? v3locationpoint { get; set; }

        [JsonPropertyName("v3-wx-forecast-daily-3day")]
        public MoonTimes? v3wxforecastdaily3day { get; set; }
    }

    /// <summary>Only the moonrise and moonset times of the daily forecast. Both lists can hold nulls.</summary>
    public class MoonTimes
    {
        public List<DateTimeOffset?>? moonriseTimeLocal { get; set; }
        public List<DateTimeOffset?>? moonsetTimeLocal { get; set; }
    }
}
