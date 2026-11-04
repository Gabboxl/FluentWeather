using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FluentWeather.Utils
{
    internal class ApiUtils
    {
        // The language chosen in the app settings, otherwise the Windows display language.
        private static readonly string SystemLanguage = !string.IsNullOrEmpty(Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride)
            ? Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride
            : Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

        private static readonly HttpClient SharedClient = CreateClient("https://api.weather.com/v2/");

        /// <summary>
        /// An HttpClient that does not keep idle connections for long. While the app is suspended
        /// (window minimized or PC asleep), the network can close an idle connection without the app
        /// noticing. The next request on it then fails with "An existing connection was forcibly
        /// closed by the remote host". Idle connections are closed after 15 seconds, and every
        /// connection is replaced after 2 minutes.
        /// </summary>
        internal static HttpClient CreateClient(string baseAddress)
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            };
            return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
        }

        /// <summary>
        /// Sends a GET request. When the connection fails before the server answers, the request is
        /// sent once more after 0.2 seconds. The failed connection is dropped, so the second try
        /// uses a new one. This covers the first request after the app wakes up.
        /// An answer with an error status (for example 404) is returned and not repeated.
        /// </summary>
        internal static async Task<HttpResponseMessage> GetWithRetryAsync(HttpClient client, string url)
        {
            try
            {
                return await client.GetAsync(url);
            }
            catch (HttpRequestException e) when (e.StatusCode == null)
            {
                await Task.Delay(200);
                return await client.GetAsync(url);
            }
        }

        public static async Task<HttpResponseMessage> GetFullData(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
                throw new ArgumentNullException(nameof(placeId));

            var response = await GetWithRetryAsync(SharedClient,
                "aggcommon/v3alertsHeadlines;v3-wx-observations-current;v3-wx-forecast-hourly-10day;v3-wx-forecast-daily-10day;v3-wx-forecast-daily-15day;v3-location-point;v2idxDrySkinDaypart10;v2idxWateringDaypart10;v2idxPollenDaypart10;v2idxRunDaypart10;v2idxDriveDaypart10?format=json&placeid="
                + placeId
                + "&units=" + await VariousUtils.GetUnitsCode()
                + "&language=" +
                SystemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
            //&locationType=city (x solo citta)

            response.EnsureSuccessStatusCode();

            return response;
        }

        /// <summary>
        /// The nearest official weather station (usually an airport METAR station) to a point.
        /// Returns name, station id, distance in km and distance in miles, or null.
        /// The current conditions of weather.com are a blend for the exact point. This station is
        /// only the nearest one that reports, shown for reference.
        /// </summary>
        public static async Task<string[]?> GetNearestStation(double latitude, double longitude)
        {
            var url = "https://api.weather.com/v3/location/near?geocode="
                      + latitude.ToString("0.###", CultureInfo.InvariantCulture) + ","
                      + longitude.ToString("0.###", CultureInfo.InvariantCulture)
                      + "&product=observation&format=json&apiKey=793db2b6128c4bc2bdb2b6128c0bc230";

            using var response = await GetWithRetryAsync(SharedClient, url);
            if (!response.IsSuccessStatusCode)
                return null;

            // JsonDocument needs no generated types, so it works with the trimmed (AOT) build.
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("location", out var location))
                return null;

            string? First(string name)
            {
                if (!location.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
                    return null;
                var item = array[0];
                return item.ValueKind switch
                {
                    JsonValueKind.String => item.GetString(),
                    JsonValueKind.Number => item.GetDouble().ToString("0.0", CultureInfo.InvariantCulture),
                    _ => null
                };
            }

            var stationName = First("stationName");
            var stationId = First("stationId");
            var km = First("distanceKm");
            var mi = First("distanceMi");
            if (string.IsNullOrEmpty(stationName) || string.IsNullOrEmpty(stationId) || km == null || mi == null)
                return null;

            return [stationName, stationId, km, mi];
        }

        /// <summary>
        /// Current conditions, place name and the 3-day forecast (for moonrise and moonset), about 6 KB.
        /// Much smaller than GetFullData. If the request with the forecast fails, it is sent again
        /// without it, so the current conditions still arrive.
        /// </summary>
        public static async Task<HttpResponseMessage> GetCurrentConditions(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
                throw new ArgumentNullException(nameof(placeId));

            var query = "?format=json&placeid=" + Uri.EscapeDataString(placeId)
                        + "&units=" + await VariousUtils.GetUnitsCode()
                        + "&language=" + SystemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230";

            var response = await GetWithRetryAsync(SharedClient, "aggcommon/v3-wx-observations-current;v3-location-point;v3-wx-forecast-daily-3day" + query);
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                response = await GetWithRetryAsync(SharedClient, "aggcommon/v3-wx-observations-current;v3-location-point" + query);
            }

            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}
