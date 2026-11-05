using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentWeather.Helpers;
using FluentWeather.Models;
using FluentWeather.Utils;
using Windows.Storage;

namespace FluentWeather.Services
{
    /// <summary>
    /// Writes current conditions for the FluentWeather tray icons program, so that program itself
    /// never needs internet access. Nothing is written while the "Tray icons" setting is off. Two files in LocalState, UTF-8, one "key=value" per line:
    ///
    ///   tray-weather.txt       the location shown in the app
    ///   tray-weather-all.txt   every saved location, each block starting with "[placeId]"
    ///
    /// Lines starting with "d." are ready-to-show details for the tray's hover panel,
    /// for example "d.Humidity=96%". The tray lets the user choose which ones to show.
    ///
    /// Both are written by the app after loading data, and by the live tile background task,
    /// which runs about every 15 minutes even when the app is closed.
    /// </summary>
    internal static class TrayWeatherFile
    {
        private const string CurrentFileName = "tray-weather.txt";
        private const string AllFileName = "tray-weather-all.txt";

        // Do not refresh all saved locations more often than this, unless the list changed.
        private static readonly TimeSpan AllRefreshInterval = TimeSpan.FromMinutes(10);
        private const int MaxParallelRequests = 4;

        private static readonly SemaphoreSlim WriteLock = new(1, 1);
        private static readonly SemaphoreSlim AllLock = new(1, 1);
        private static DateTimeOffset _lastAllRefresh = DateTimeOffset.MinValue;

        /// <summary>Writes the location shown in the app.</summary>
        public static async Task TryWriteAsync(RootV3Response data)
        {
            if (!TrayIconsService.IsEnabled)
                return;
            try
            {
                var units = await VariousUtils.GetUnitsCode();
                var daily = data.v3wxforecastdaily10day;
                var text = await FormatBlockAsync(data.v3wxobservationscurrent, data.v3locationpoint.LocationV3, units,
                    daily?.moonriseTimeLocal, daily?.moonsetTimeLocal);
                await WriteFileAsync(CurrentFileName, text);
            }
            catch (Exception)
            {
                // The tray display is optional. A failed write must never affect the app or the live tile.
            }
        }

        /// <summary>
        /// Fetches the current conditions of the location shown in the app (the "lastPlaceId" setting)
        /// with the small request, and writes tray-weather.txt. Used by the tray's 5-minute refresh.
        /// </summary>
        public static async Task RefreshCurrentAsync()
        {
            if (!TrayIconsService.IsEnabled)
                return;
            try
            {
                var placeId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");
                if (string.IsNullOrEmpty(placeId))
                    return;

                var response = await ApiUtils.GetCurrentConditions(placeId);
                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize(json, FluentWeatherJsonContext.Default.CurrentConditionsResponse);
                if (data?.v3wxobservationscurrent == null || data.v3locationpoint?.LocationV3 == null)
                    return;

                var units = await VariousUtils.GetUnitsCode();
                var text = await FormatBlockAsync(data.v3wxobservationscurrent, data.v3locationpoint.LocationV3, units,
                    data.v3wxforecastdaily3day?.moonriseTimeLocal, data.v3wxforecastdaily3day?.moonsetTimeLocal);
                await WriteFileAsync(CurrentFileName, text);
            }
            catch (Exception)
            {
                // The tray keeps its last data. The next refresh tries again.
            }
        }

        /// <summary>
        /// Fetches current conditions for every saved location and writes tray-weather-all.txt.
        /// A location whose request fails keeps its previous data.
        /// </summary>
        public static async Task RefreshAllSavedAsync(bool force)
        {
            if (!TrayIconsService.IsEnabled)
                return;
            if (!await AllLock.WaitAsync(0))
                return; // a refresh is already running
            try
            {
                if (!force && DateTimeOffset.UtcNow - _lastAllRefresh < AllRefreshInterval)
                    return;

                var saved = await SavedLocationsService.GetAllAsync();
                var previous = await ReadPreviousBlocksAsync();
                var units = await VariousUtils.GetUnitsCode();

                using var throttle = new SemaphoreSlim(MaxParallelRequests);
                var tasks = saved.Select(async location =>
                {
                    await throttle.WaitAsync();
                    try
                    {
                        var response = await ApiUtils.GetCurrentConditions(location.PlaceId);
                        var json = await response.Content.ReadAsStringAsync();
                        var data = System.Text.Json.JsonSerializer.Deserialize(json, FluentWeatherJsonContext.Default.CurrentConditionsResponse);
                        if (data?.v3wxobservationscurrent == null || data.v3locationpoint?.LocationV3 == null)
                            return previous.TryGetValue(location.PlaceId, out var old) ? old : null;

                        return "[" + location.PlaceId + "]\n" +
                               await FormatBlockAsync(data.v3wxobservationscurrent, data.v3locationpoint.LocationV3, units,
                                   data.v3wxforecastdaily3day?.moonriseTimeLocal, data.v3wxforecastdaily3day?.moonsetTimeLocal);
                    }
                    catch (Exception)
                    {
                        return previous.TryGetValue(location.PlaceId, out var old) ? old : null;
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }).ToList();

                var blocks = await Task.WhenAll(tasks);
                var text = string.Join("\n", blocks.Where(b => !string.IsNullOrEmpty(b)));
                await WriteFileAsync(AllFileName, text);
                _lastAllRefresh = DateTimeOffset.UtcNow;
            }
            catch (Exception)
            {
                // Optional feature. Try again at the next refresh.
            }
            finally
            {
                AllLock.Release();
            }
        }

        // The nearest official weather station does not change, so it is looked up once per
        // location and kept in the app settings for this long.
        private static readonly TimeSpan StationCacheAge = TimeSpan.FromDays(30);

        // "d.Station=Fairfield (KFFL), 3.7 mi", or an empty string when it cannot be found.
        private static async Task<string> FormatStationAsync(LocationV3 loc, WetUnits units)
        {
            try
            {
                var key = "nearStation|" + loc.latitude.ToString("0.00", CultureInfo.InvariantCulture) + ","
                          + loc.longitude.ToString("0.00", CultureInfo.InvariantCulture);
                var settings = ApplicationData.Current.LocalSettings.Values;
                var parts = (settings.TryGetValue(key, out var stored) ? stored as string : null)?.Split('|');

                var fresh = parts is { Length: 5 }
                            && long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                            && DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < StationCacheAge;
                if (!fresh)
                {
                    var found = await ApiUtils.GetNearestStation(loc.latitude, loc.longitude);
                    if (found != null)
                    {
                        parts = found.Select(x => x.Replace('|', '/')).Append(DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)).ToArray();
                        settings[key] = string.Join("|", parts);
                    }
                }

                if (parts is not { Length: 5 })
                    return string.Empty;

                var distance = units is WetUnits.e or WetUnits.h ? parts[3] + " mi" : parts[2] + " km";
                return "d.Station=" + OneLine(parts[0] + " (" + parts[1] + "), " + distance) + "\n";
            }
            catch (Exception)
            {
                // The station line is optional.
                return string.Empty;
            }
        }

        private static async Task<string> FormatBlockAsync(V3WxObservationsCurrent obs, LocationV3 loc, WetUnits units,
            List<DateTimeOffset?>? moonrises = null, List<DateTimeOffset?>? moonsets = null)
        {
            var place = new[] { loc.displayName, loc.city }.FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty;
            var fullPlace = string.Join(", ", new[] { loc.displayName, loc.adminDistrict, loc.country }
                .Where(s => !string.IsNullOrEmpty(s)).Distinct());

            return new StringBuilder()
                .Append("place=").Append(OneLine(place)).Append('\n')
                .Append("fullplace=").Append(OneLine(fullPlace)).Append('\n')
                .Append("temp=").Append(obs.temperature).Append('\n')
                .Append("feels=").Append(obs.temperatureFeelsLike).Append('\n')
                .Append("unit=").Append(MeasureUnitUtils.GetTemperatureUnits(units)).Append('\n')
                .Append("icon=").Append(obs.iconCode).Append('\n')
                // "D" or "N". weather.com sends the same "Cloudy" code by day and by night, so the tray
                // needs this to show the moon behind the cloud at night.
                .Append("daynight=").Append(OneLine(obs.dayOrNight ?? string.Empty)).Append('\n')
                // The tray draws the moon as it looks from this latitude (lit side mirrored south of the equator).
                .Append("lat=").Append(loc.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n')
                .Append("phrase=").Append(OneLine(obs.wxPhraseLong ?? obs.cloudCoverPhrase)).Append('\n')
                .Append("observed=").Append(obs.validTimeLocal.ToString("o")).Append('\n')
                .Append("written=").Append(DateTimeOffset.UtcNow.ToString("o")).Append('\n')
                .Append(await FormatDetailsAsync(obs, units))
                .Append(await FormatMoonTimeAsync("Moonrise", moonrises))
                .Append(await FormatMoonTimeAsync("Moonset", moonsets))
                .Append(await FormatStationAsync(loc, units))
                .ToString();
        }

        // Every current-conditions value the service provides, as display text in the user's units.
        private static async Task<string> FormatDetailsAsync(V3WxObservationsCurrent obs, WetUnits units)
        {
            var c = CultureInfo.CurrentCulture;
            var deg = "\u00B0" + MeasureUnitUtils.GetTemperatureUnits(units);
            var imperial = units == WetUnits.e;
            var rain = MeasureUnitUtils.GetLiquidPrecipitationUnits(units);
            var snow = MeasureUnitUtils.GetSnowPrecipitationUnits(units);
            var sb = new StringBuilder();

            void Add(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    sb.Append("d.").Append(key).Append('=').Append(OneLine(value)).Append('\n');
            }

            string Num(double v) => v.ToString("0.##", c);

            Add("Conditions", obs.wxPhraseLong ?? obs.cloudCoverPhrase);
            Add("Temperature", obs.temperature + deg);
            Add("FeelsLike", obs.temperatureFeelsLike + deg);
            Add("HighLow", obs.temperatureMax24Hour + "\u00B0 / " + obs.temperatureMin24Hour + "\u00B0");
            Add("Change24h", (obs.temperatureChange24Hour > 0 ? "+" : string.Empty) + obs.temperatureChange24Hour + "\u00B0");
            Add("DewPoint", obs.temperatureDewPoint + deg);
            Add("HeatIndex", obs.temperatureHeatIndex + deg);
            Add("WindChill", obs.temperatureWindChill + deg);
            Add("Humidity", obs.relativeHumidity + "%");

            var wind = (obs.windDirectionCardinal + " " + obs.windSpeed + " " + MeasureUnitUtils.GetWindSpeedUnits(units)).Trim();
            if (obs.windGust.HasValue)
                wind += ", gusts " + obs.windGust.Value;
            Add("Wind", wind);

            Add("UV", obs.uvIndex + (string.IsNullOrEmpty(obs.uvDescription) ? string.Empty : " (" + obs.uvDescription + ")"));

            // Imperial: altimeter setting in inHg, as US forecasts show it. Otherwise sea-level pressure in mb.
            var pressure = imperial ? Num(obs.pressureAltimeter) + " inHg" : Num(obs.pressureMeanSeaLevel) + " mb";
            if (!string.IsNullOrEmpty(obs.pressureTendencyTrend))
                pressure += ", " + obs.pressureTendencyTrend.ToLowerInvariant();
            Add("Pressure", pressure);

            Add("Visibility", Num(obs.visibility) + " " + MeasureUnitUtils.GetVisibilityUnits(units));
            if (obs.cloudCeiling.HasValue)
                Add("CloudCeiling", obs.cloudCeiling.Value.ToString("N0", c) + " " + MeasureUnitUtils.GetAltitudeUnits(units));
            Add("Rain1h", Num(obs.precip1Hour) + " " + rain);
            Add("Rain6h", Num(obs.precip6Hour) + " " + rain);
            Add("Rain24h", Num(obs.precip24Hour) + " " + rain);
            Add("Snow24h", Num(obs.snow24Hour) + " " + snow);
            Add("Sunrise", await VariousUtils.GetTimeBasedOnUserSettings(obs.sunriseTimeLocal.DateTime));
            Add("Sunset", await VariousUtils.GetTimeBasedOnUserSettings(obs.sunsetTimeLocal.DateTime));
            Add("Observed", await VariousUtils.GetTimeBasedOnUserSettings(obs.validTimeLocal.DateTime));
            return sb.ToString();
        }

        // The next moonrise or moonset that is still to come, for example "d.Moonrise=1:51 AM".
        // A time on a later day starts with the weekday, for example "d.Moonset=Thu 2:38 PM".
        // The forecast list can start with the day before, and a day can have no moonrise at all.
        private static async Task<string> FormatMoonTimeAsync(string key, List<DateTimeOffset?>? times)
        {
            if (times == null)
                return string.Empty;

            var now = DateTimeOffset.UtcNow;
            foreach (var entry in times)
            {
                if (entry is not DateTimeOffset time || time <= now)
                    continue;

                var text = await VariousUtils.GetTimeBasedOnUserSettings(time.DateTime);
                if (time.Date != now.ToOffset(time.Offset).Date)
                    text = time.ToString("ddd", CultureInfo.CurrentCulture) + " " + text;
                return "d." + key + "=" + OneLine(text) + "\n";
            }
            return string.Empty;
        }

        // Reads the existing all-locations file, so a failed request can keep the last good data.
        private static async Task<Dictionary<string, string>> ReadPreviousBlocksAsync()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, AllFileName);
                if (!File.Exists(path))
                    return result;

                string? id = null;
                var block = new StringBuilder();
                foreach (var line in await File.ReadAllLinesAsync(path, Encoding.UTF8))
                {
                    if (line.StartsWith('[') && line.EndsWith(']'))
                    {
                        if (id != null) result[id] = block.ToString();
                        id = line.Substring(1, line.Length - 2);
                        block.Clear().Append(line).Append('\n');
                    }
                    else if (id != null)
                    {
                        block.Append(line).Append('\n');
                    }
                }
                if (id != null) result[id] = block.ToString();
            }
            catch (Exception)
            {
                // No previous data.
            }
            return result;
        }

        private static async Task WriteFileAsync(string fileName, string text)
        {
            await WriteLock.WaitAsync();
            try
            {
                var folder = ApplicationData.Current.LocalFolder.Path;
                var tmp = Path.Combine(folder, fileName + ".tmp");
                var dst = Path.Combine(folder, fileName);

                // Write to a temporary file first, so the tray icons program never reads a half-written file.
                await File.WriteAllTextAsync(tmp, text, new UTF8Encoding(false));
                File.Move(tmp, dst, true);
            }
            finally
            {
                WriteLock.Release();
            }
        }

        private static string OneLine(string? value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
