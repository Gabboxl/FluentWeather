using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentWeather.Utils
{
    internal class VariousUtils
    {
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
    }
}
