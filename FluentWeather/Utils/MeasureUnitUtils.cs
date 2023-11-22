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
                WetUnits.m => "m",
                WetUnits.e => "ft",
                WetUnits.h => "ft",
                WetUnits.s => "m",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetTemperatureUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "C",
                WetUnits.e => "F",
                WetUnits.h => "C",
                WetUnits.s => "C",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetPressureUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "mb/hPa",
                WetUnits.e => "hg",
                WetUnits.h => "mb",
                WetUnits.s => "mb",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetLiquidPrecipitationUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "mm",
                WetUnits.e => "in",
                WetUnits.h => "mm",
                WetUnits.s => "mm",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetSnowPrecipitationUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "cm",
                WetUnits.e => "in",
                WetUnits.h => "cm",
                WetUnits.s => "cm",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetDistanceUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "km",
                WetUnits.e => "mi",
                WetUnits.h => "mi",
                WetUnits.s => "m",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetVisibilityUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "km",
                WetUnits.e => "mi",
                WetUnits.h => "km",
                WetUnits.s => "km",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetWindSpeedUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "km/h",
                WetUnits.e => "mph",
                WetUnits.h => "mph",
                WetUnits.s => "m/s",
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }

        public static string GetWaveHeightUnits(WetUnits wetUnits)
        {
            return wetUnits switch
            {
                WetUnits.m => "mtr", //???
                WetUnits.e => "ft",
                WetUnits.h => "ft",
                WetUnits.s => "mtr", //??
                _ => throw new ArgumentOutOfRangeException(nameof(wetUnits), wetUnits, null)
            };
        }
    }
}
