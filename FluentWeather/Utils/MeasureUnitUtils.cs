using FluentWeather.Models;
using System;

namespace FluentWeather.Utils
{
    internal class MeasureUnitUtils
    {
        public static string GetAltitudeUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "ft",
                WetUnits.m => "m",
                WetUnits.s => "m",
                WetUnits.h => "ft",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetTemperatureUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "F",
                WetUnits.m => "C",
                WetUnits.s => "C",
                WetUnits.h => "C",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetPressureUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "hg",
                WetUnits.m => "mb/hPa",
                WetUnits.s => "mb",
                WetUnits.h => "mb",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetPrecipitationUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "in",
                WetUnits.m => "mm", //TODO: beware that in case of snow this is cm (centimeters), instead of mm (millimeters)
                WetUnits.s => "mm", //TODO: beware that in case of snow this is cm (centimeters), instead of mm (millimeters)
                WetUnits.h => "mm", //SAME THING for hybrid
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetDistanceUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "mi",
                WetUnits.m => "km",
                WetUnits.s => "m",
                WetUnits.h => "mi",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetVisibilityUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "mi",
                WetUnits.m => "km",
                WetUnits.s => "km",
                WetUnits.h => "km",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetWindSpeedUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "mph",
                WetUnits.m => "km/h",
                WetUnits.s => "m/s",
                WetUnits.h => "mph",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetWaveHeightUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.e => "ft",
                WetUnits.m => "mtr", //???
                WetUnits.s => "mtr", //??
                WetUnits.h => "ft",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }
    }
}
