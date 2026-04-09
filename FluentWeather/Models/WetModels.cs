using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WinRT;

namespace FluentWeather.Models
{
    //list of units
    public enum WetUnits
    {
        m = 0, //metric
        e = 1, //imperial
        h = 2, //hybrid
        s = 3 //metric SI
    }

    public class Locale
    {
        public required string locale1 { get; set; }
        public required string locale2 { get; set; }
        public required object locale3 { get; set; }
        public required object locale4 { get; set; }
    }

    [GeneratedBindableCustomPropertyAttribute]
    public partial class SearchedLocation
    {
        public required string address { get; set; }
        public required Locale locale { get; set; }
        public required string displayName { get; set; }
        public required string ianaTimeZone { get; set; }
        public required string adminDistrict { get; set; }
        public required object adminDistrictCode { get; set; }
        public required string city { get; set; }
        public required double longitude { get; set; }
        public required string postalCode { get; set; }
        public required double latitude { get; set; }
        public required string placeId { get; set; }
        public required object neighborhood { get; set; }
        public required string country { get; set; }
        public required string postalKey { get; set; }
        public required string countryCode { get; set; }
        public required bool disputedArea { get; set; }
        public required object disputedCountries { get; set; }
        public required object disputedCountryCodes { get; set; }
        public required object disputedCustomers { get; set; }
        public required List<bool> disputedShowCountry { get; set; }
        public required string iataCode { get; set; }
        public required string icaoCode { get; set; }
        public required string locId { get; set; }
        public required object locationCategory { get; set; }
        public required string pwsId { get; set; }
        public required string type { get; set; }
    }

    public class SearchLocationResponse
    {
        public required List<SearchedLocation> location { get; set; }
    }

    public class Daypart
    {
        public required List<int?> cloudCover { get; set; }
        public required List<string> dayOrNight { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int?> iconCode { get; set; }
        public required List<int?> iconCodeExtend { get; set; }
        public required List<string> narrative { get; set; }
        public required List<int?> precipChance { get; set; }
        public required List<string> precipType { get; set; }
        public required List<double?> qpf { get; set; }
        public required List<double?> qpfSnow { get; set; }
        public required List<string> qualifierCode { get; set; }
        public required List<string> qualifierPhrase { get; set; }
        public required List<int?> relativeHumidity { get; set; }
        public required List<string> snowRange { get; set; }
        public required List<int?> temperature { get; set; }
        public required List<int?> temperatureHeatIndex { get; set; }
        public required List<int?> temperatureWindChill { get; set; }
        public required List<string> thunderCategory { get; set; }
        public required List<int?> thunderIndex { get; set; }
        public required List<string> uvDescription { get; set; }
        public required List<int?> uvIndex { get; set; }
        public required List<int?> windDirection { get; set; }
        public required List<string> windDirectionCardinal { get; set; }
        public required List<string> windPhrase { get; set; }
        public required List<int?> windSpeed { get; set; }
        public required List<string> wxPhraseLong { get; set; }
        public required List<string> wxPhraseShort { get; set; }
    }


    public class LocationV3
    {
        public required double latitude { get; set; }
        public required double longitude { get; set; }
        public required string city { get; set; }
        public required Locale locale { get; set; }
        public required string neighborhood { get; set; }
        public required string adminDistrict { get; set; }
        public required string adminDistrictCode { get; set; }
        public required string postalCode { get; set; }
        public required string postalKey { get; set; }
        public required string country { get; set; }
        public required string countryCode { get; set; }
        public required string ianaTimeZone { get; set; }
        public required string displayName { get; set; }
        public required DateTimeOffset? dstEnd { get; set; }
        public required DateTimeOffset? dstStart { get; set; }
        public required string dmaCd { get; set; }
        public required string placeId { get; set; }
        public required bool disputedArea { get; set; }
        public required object disputedCountries { get; set; }
        public required object disputedCountryCodes { get; set; }
        public required object disputedCustomers { get; set; }
        public required List<bool> disputedShowCountry { get; set; }
        public required string canonicalCityId { get; set; }
        public required string countyId { get; set; }
        public required string locId { get; set; }
        public required object locationCategory { get; set; }
        public required string pollenId { get; set; }
        public required string pwsId { get; set; }
        public required string regionalSatellite { get; set; }
        public required object tideId { get; set; }
        public required string type { get; set; }
        public required string zoneId { get; set; }
    }

    public class RootV3Response
    {
        public string id { get; set; }

        [JsonPropertyName("v3-wx-observations-current")]
        public required V3WxObservationsCurrent v3wxobservationscurrent { get; set; }

        [JsonPropertyName("v3-wx-forecast-daily-15day")]
        public required V3WxForecastDaily v3wxforecastdaily15day { get; set; }

        [JsonPropertyName("v3-wx-forecast-daily-10day")]
        public required V3WxForecastDaily v3wxforecastdaily10day { get; set; }

        [JsonPropertyName("v3-wx-forecast-hourly-10day")]
        public required V3WxForecastHourly v3wxforecasthourly10day { get; set; }

        public required object v3alertsHeadlines { get; set; }

        [JsonPropertyName("v3-location-point")]
        public required V3LocationPoint v3locationpoint { get; set; }

        [JsonPropertyName("v2idxDriveDaypart10")]
        public required V2idxDriveDaypartResult v2idxDriveDaypart10days { get; set; }

        [JsonPropertyName("v2idxRunDaypart10")]
        public required V2idxRunDaypartResult v2idxRunDaypart10days { get; set; }

        [JsonPropertyName("v2idxPollenDaypart10")]
        public required V2idxPollenDaypartResult v2idxPollenDaypart10days { get; set; }

        [JsonPropertyName("v2idxWateringDaypart10")]
        public required V2idxWateringDaypartResult V2IdxWateringDaypart10days { get; set; }

        [JsonPropertyName("v2idxDrySkinDaypart10")]
        public required V2idxDrySkinDaypartResult V2IdxDrySkinDaypart10days { get; set; }
    }


    public class V3WxForecastHourly
    {
        public required List<int> cloudCover { get; set; }
        public required List<string> dayOfWeek { get; set; }
        public required List<string> dayOrNight { get; set; }
        public required List<int> expirationTimeUtc { get; set; }
        public required List<int> iconCode { get; set; }
        public required List<int> iconCodeExtend { get; set; }
        public required List<int> precipChance { get; set; }
        public required List<string> precipType { get; set; }
        public required List<double> pressureMeanSeaLevel { get; set; }
        public required List<double> qpf { get; set; }
        public required List<double> qpfSnow { get; set; }
        public required List<int> relativeHumidity { get; set; }
        public required List<int> temperature { get; set; }
        public required List<int> temperatureDewPoint { get; set; }
        public required List<int?> temperatureFeelsLike { get; set; }
        public required List<int> temperatureHeatIndex { get; set; }
        public required List<int> temperatureWindChill { get; set; }
        public required List<string> uvDescription { get; set; }
        public required List<int> uvIndex { get; set; }
        public required List<DateTimeOffset> validTimeLocal { get; set; }
        public required List<int> validTimeUtc { get; set; }
        public required List<double> visibility { get; set; }
        public required List<int> windDirection { get; set; }
        public required List<string> windDirectionCardinal { get; set; }
        public required List<int?> windGust { get; set; }
        public required List<int> windSpeed { get; set; }
        public required List<string> wxPhraseLong { get; set; }
        public required List<string> wxPhraseShort { get; set; }
        public required List<int> wxSeverity { get; set; }
    }


    public class RootStandaloneHourlyResponse
    {
        [JsonPropertyName("v3-wx-forecast-hourly-10day")]
        public required V3WxForecastHourly v3wxforecasthourly10day { get; set; }
    }

    public class V3LocationPoint
    {
        [JsonPropertyName("location")]
        public required LocationV3 LocationV3 { get; set; }
    }

    public class V3WxForecastDaily
    {
        public required List<int?> calendarDayTemperatureMax { get; set; }
        public required List<int> calendarDayTemperatureMin { get; set; }
        public required List<string> dayOfWeek { get; set; }
        public required List<int> expirationTimeUtc { get; set; }
        public required List<string> moonPhase { get; set; }
        public required List<string> moonPhaseCode { get; set; }
        public required List<int> moonPhaseDay { get; set; }
        public required List<DateTimeOffset?> moonriseTimeLocal { get; set; }
        public required List<int?> moonriseTimeUtc { get; set; }
        public required List<DateTimeOffset?> moonsetTimeLocal { get; set; }
        public required List<int?> moonsetTimeUtc { get; set; }
        public required List<string> narrative { get; set; }
        public required List<double?> qpf { get; set; }
        public required List<double> qpfSnow { get; set; }
        public required List<DateTimeOffset?> sunriseTimeLocal { get; set; }
        public required List<int?> sunriseTimeUtc { get; set; }
        public required List<DateTimeOffset?> sunsetTimeLocal { get; set; }
        public required List<int?> sunsetTimeUtc { get; set; }
        public required List<int?> temperatureMax { get; set; }
        public required List<int> temperatureMin { get; set; }
        public required List<DateTimeOffset> validTimeLocal { get; set; }
        public required List<int> validTimeUtc { get; set; }
        public required List<Daypart> daypart { get; set; }
    }


    public class V3WxObservationsCurrent
    {
        public required int? cloudCeiling { get; set; }
        public required string cloudCoverPhrase { get; set; }
        public required string dayOfWeek { get; set; }
        public required string dayOrNight { get; set; }
        public required int expirationTimeUtc { get; set; }
        public required int iconCode { get; set; }
        public required int iconCodeExtend { get; set; }
        public required object obsQualifierCode { get; set; }
        public required object obsQualifierSeverity { get; set; }
        public required double precip1Hour { get; set; }
        public required double precip6Hour { get; set; }
        public required double precip24Hour { get; set; }
        public required double pressureAltimeter { get; set; }
        public required double pressureChange { get; set; }
        public required double pressureMeanSeaLevel { get; set; }
        public required int pressureTendencyCode { get; set; }
        public required string pressureTendencyTrend { get; set; }
        public required int relativeHumidity { get; set; }
        public required double snow1Hour { get; set; }
        public required double snow6Hour { get; set; }
        public required double snow24Hour { get; set; }
        public required DateTimeOffset sunriseTimeLocal { get; set; }
        public required int sunriseTimeUtc { get; set; }
        public required DateTimeOffset sunsetTimeLocal { get; set; }
        public required int sunsetTimeUtc { get; set; }
        public required int temperature { get; set; }
        public required int temperatureChange24Hour { get; set; }
        public required int temperatureDewPoint { get; set; }
        public required int temperatureFeelsLike { get; set; }
        public required int temperatureHeatIndex { get; set; }
        public required int temperatureMax24Hour { get; set; }
        public required int temperatureMaxSince7Am { get; set; }
        public required int temperatureMin24Hour { get; set; }
        public required int temperatureWindChill { get; set; }
        public required string uvDescription { get; set; }
        public required int uvIndex { get; set; }
        public required DateTimeOffset validTimeLocal { get; set; }
        public required int validTimeUtc { get; set; }
        public required double visibility { get; set; }
        public required int windDirection { get; set; }
        public required string windDirectionCardinal { get; set; }
        public required int? windGust { get; set; }
        public required int windSpeed { get; set; }
        public required string wxPhraseLong { get; set; }
        public required object wxPhraseMedium { get; set; }
        public required object wxPhraseShort { get; set; }
    }


    //driving difficulty index

    public class DrivingDifficultyIndexDaypart
    {
        public required List<int> fcstValid { get; set; }
        public required List<DateTimeOffset> fcstValidLocal { get; set; }
        public required List<string> dayInd { get; set; }
        public required List<int> num { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int> drivingDifficultyIndex { get; set; }
        public required List<string> drivingDifficultyCategory { get; set; }
    }

    public class Metadata
    {
        public required string language { get; set; }
        public required string transactionId { get; set; }
        public required string version { get; set; }
        public required double latitude { get; set; }
        public required double longitude { get; set; }
        public required int expireTimeGmt { get; set; }
        public required int statusCode { get; set; }
    }

    public class V2idxDriveDaypartResult
    {
        public required Metadata metadata { get; set; }

        [JsonPropertyName("drivingDifficultyIndex12hour")]
        public required DrivingDifficultyIndexDaypart drivingDifficultyIndex12hour { get; set; }
    }


    //running index

    public class RunWeatherIndexDaypart
    {
        public required List<int> fcstValid { get; set; }
        public required List<DateTimeOffset> fcstValidLocal { get; set; }
        public required List<string> dayInd { get; set; }
        public required List<int> num { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int> longRunWeatherIndex { get; set; }
        public required List<string> longRunWeatherCategory { get; set; }
        public required List<int> shortRunWeatherIndex { get; set; }
        public required List<string> shortRunWeatherCategory { get; set; }
    }

    public class V2idxRunDaypartResult
    {
        public required Metadata metadata { get; set; }

        [JsonPropertyName("runWeatherIndex12hour")]
        public required RunWeatherIndexDaypart RunWeatherIndexDaypart { get; set; }
    }


    //pollen index

    public class PollenForecastDaypart
    {
        public required List<int> fcstValid { get; set; }
        public required List<DateTimeOffset> fcstValidLocal { get; set; }
        public required List<string> dayInd { get; set; }
        public required List<int> num { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int> grassPollenIndex { get; set; }
        public required List<string> grassPollenCategory { get; set; }
        public required List<int> treePollenIndex { get; set; }
        public required List<string> treePollenCategory { get; set; }
        public required List<int> ragweedPollenIndex { get; set; }
        public required List<string> ragweedPollenCategory { get; set; }
    }


    public class V2idxPollenDaypartResult
    {
        public required Metadata metadata { get; set; }

        [JsonPropertyName("pollenForecast12hour")]
        public required PollenForecastDaypart PollenForecastDaypart { get; set; }
    }


    //watering needs index

    public class V2idxWateringDaypartResult
    {
        public required Metadata metadata { get; set; }

        [JsonPropertyName("wateringNeedsIndex12hour")]
        public required WateringNeedsIndexDaypart WateringNeedsIndexDaypart { get; set; }
    }

    public class WateringNeedsIndexDaypart
    {
        public required List<int> fcstValid { get; set; }
        public required List<DateTimeOffset> fcstValidLocal { get; set; }
        public required List<string> dayInd { get; set; }
        public required List<int> num { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int> wateringNeedsIndex { get; set; }
        public required List<string> wateringNeedsCategory { get; set; }
    }


    //dry skin index

    public class DrySkinIndexDaypart
    {
        public required List<int> fcstValid { get; set; }
        public required List<DateTimeOffset> fcstValidLocal { get; set; }
        public required List<string> dayInd { get; set; }
        public required List<int> num { get; set; }
        public required List<string> daypartName { get; set; }
        public required List<int> drySkinIndex { get; set; }
        public required List<string> drySkinCategory { get; set; }
    }

    public class V2idxDrySkinDaypartResult
    {
        public required Metadata metadata { get; set; }

        [JsonPropertyName("drySkinIndex12hour")]
        public required DrySkinIndexDaypart DrySkinIndexDaypart { get; set; }
    }
}
