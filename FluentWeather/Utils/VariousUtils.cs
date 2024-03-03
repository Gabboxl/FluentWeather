using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Storage;
using FluentWeather.Helpers;
using FluentWeather.Models;

namespace FluentWeather.Utils
{
    internal class VariousUtils
    {
        public static bool IsWindows11()
        {
            return Environment.OSVersion.Version.Build >= 22000;
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = s.ToLower();
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }


        public static string GetAbbreviatedDayName(string localDayName)
        {
            // Get the current culture's DateTimeFormatInfo
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;

            // Find the day index from the full day name
            int dayIndex = Array.IndexOf(formatInfo.DayNames, localDayName);

            // If the day index is valid, get the abbreviated day name
            if (dayIndex >= 0 && dayIndex < formatInfo.AbbreviatedDayNames.Length)
            {
                return formatInfo.AbbreviatedDayNames[dayIndex];
            }

            // Return an empty string if the day name is not found
            return string.Empty;
        }

        public static async Task<WetUnits> GetUnitsCode()
        {
            WetUnits unitsCode =
                (WetUnits) await ApplicationData.Current.LocalSettings.ReadAsync<int>("selectedUnits");

            return unitsCode;
        }

        public static async Task<string> GetTimeBasedOnUserSettings(DateTime time)
        {
            bool is12HourFormat = await ApplicationData.Current.LocalSettings.ReadAsync<bool>("is12HourFormat").ConfigureAwait(false);

            return time.ToString(is12HourFormat ? "hh:mm tt" : "HH:mm", CultureInfo.InvariantCulture);
        }

        public static string GetPrecipIconChance(int? precipChance)
        {
            string iconName = precipChance switch
            {
                < 15 or null => "blur0", 
                < 36 => "blur1", 
                < 50 => "blur2", 
                < 70 => "blur3", 
                < 85 => "blur4", 
                <= 100 => "blur5",
                _ => "blur5"
            };

            return iconName;
        }
    }
}
