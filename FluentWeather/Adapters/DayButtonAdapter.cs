using FluentWeather.Models;
using FluentWeather.Utils;
using System;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;

namespace FluentWeather.Adapters
{
    internal class DayButtonAdapter(V3WxForecastDaily fcst, int itemIndex)
    {
        public readonly V3WxForecastDaily CurrentObject = fcst;

        public readonly int ItemIndex = itemIndex;

        public string Temperature
        {
            get
            {
                var maxTemp = CurrentObject.temperatureMax[ItemIndex];
                var minTemp = CurrentObject.temperatureMin[ItemIndex];


                return (maxTemp == null ? "--" : maxTemp) + "°" + " / " + minTemp + "°";
            }
        }

        public SvgImageSource SvgImageIcon
        {
            get
            {
                var iconCode = CurrentObject.daypart[0].iconCode[ItemIndex * 2];

                if (iconCode == null && ItemIndex == 0)
                {
                    //tonight's icon
                    iconCode = CurrentObject.daypart[0].iconCode[ItemIndex * 2 + 1];
                }
                else
                {
                    //days's icon
                    iconCode = CurrentObject.daypart[0].iconCode[ItemIndex * 2];
                }

                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/weticons/" + iconCode + ".svg")
                };
            }
        }

        private int? PrecipChancePercentage
        {
            get
            {
                var precipChance = CurrentObject.daypart[0].precipChance[ItemIndex * 2];

                //if precipchange is null and it is today
                if (precipChance == null && ItemIndex == 0)
                {
                    //return "--";

                    //return this evening's precip chance
                    return CurrentObject.daypart[0].precipChance[ItemIndex * 2 + 1];
                }

                return precipChance;
            }
        }

        public string PrecipitationChance
        {
            get
            {
                return PrecipChancePercentage + "%";
            }
        }

        public SvgImageSource SvgPrecipIcon
        {
            get
            {
                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/varicons/" + VariousUtils.GetPrecipIconChance(PrecipChancePercentage) + ".svg")
                };
            }
        }

        public string ShortDayName
        {
            get
            {
                if (ItemIndex == 0)
                {
                    var dayText = CurrentObject.daypart[0].daypartName[ItemIndex * 2];

                    return (string.IsNullOrEmpty(dayText)
                        ? CurrentObject.daypart[0].daypartName[ItemIndex * 2 + 1]
                        : dayText);
                }
                else
                {
                    //get windows current culture\language
                    var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];


                    var abbname = new CultureInfo(language).DateTimeFormat.GetAbbreviatedDayName(CurrentObject
                        .validTimeLocal[ItemIndex]
                        .DayOfWeek);

                    //VariousUtils.UppercaseFirst(CurrentObject.dayOfWeek[ItemIndex]);


                    //return short day name + day complete date
                    return VariousUtils.UppercaseFirst(abbname) + " " + CurrentObject.validTimeLocal[ItemIndex].Day;
                }
            }
        }
    }
}
