using FluidWeather.Models;
using System;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;

namespace FluidWeather.Adapters
{
    internal class HourDataAdapter
    {
        public readonly V3WxForecastHourly CurrentObject;

        public readonly int ItemIndex;

        public string Temperature
        {
            get
            {
                return CurrentObject.temperature[ItemIndex] + "°";
            }
        }

        public SvgImageSource svgImageIcon
        {
            get
            {
                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/weticons/" + CurrentObject.iconCode[ItemIndex] + ".svg")
                };
            }
        }

        public string Phrase
        {
            get
            {
                return CurrentObject.wxPhraseLong[ItemIndex];
            }
        }

        public string PrecipitationChance
        {
            get
            {
                return CurrentObject.precipChance[ItemIndex] + "%";
            }
        }

        public SvgImageSource svgPrecipIcon
        {
            get
            {
                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/varicons/" + "blur" + ".svg")
                };
            }
        }

        public string Hour
        {
            get
            {
                //hour HH:mm
                return CurrentObject.validTimeLocal[ItemIndex].ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }


        public HourDataAdapter(V3WxForecastHourly fcst, int itemIndex)
        {
            CurrentObject = fcst;
            ItemIndex = itemIndex;
        }
    }
}
