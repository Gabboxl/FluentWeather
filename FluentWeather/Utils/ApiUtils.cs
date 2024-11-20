using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentWeather.Utils
{
    internal class ApiUtils
    {
        private static readonly string SystemLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

        private static readonly HttpClient SharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/v2/"),
        };

        public static async Task<HttpResponseMessage> GetFullData(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
                throw new ArgumentNullException(nameof(placeId));

            var response = await SharedClient.GetAsync(
                "aggcommon/v3-wx-observations-current;v3-wx-forecast-hourly-10day;v3-wx-forecast-daily-10day;v3-location-point;v2idxDrySkinDaypart10;v2idxWateringDaypart10;v2idxPollenDaypart10;v2idxRunDaypart10;v2idxDriveDaypart10?format=json&placeid="
                + placeId
                + "&units=" + await VariousUtils.GetUnitsCode()
                + "&language=" +
                SystemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
            //&locationType=city (x solo citta)

            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}
