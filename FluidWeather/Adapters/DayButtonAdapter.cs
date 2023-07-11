using FluidWeather.Models;
using FluidWeather.Utils;
using System;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;

namespace FluidWeather.Adapters
{
    internal class DayButtonAdapter
    {
        public readonly V3WxForecastDaily CurrentObject;

        public readonly int ItemIndex;

        public string Temperature
        {
            get
            {
                var maxTemp = CurrentObject.temperatureMax[ItemIndex];
                var minTemp = CurrentObject.temperatureMin[ItemIndex];


                return (maxTemp == null ? "--" : maxTemp)  + "°" + " / " + minTemp + "°";
            }
        }

        public SvgImageSource svgImageIcon
        {
            get
            {
                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/weticons/" + CurrentObject.daypart[0].iconCode[ItemIndex*2] + ".svg")
                };
            }
        }

        public string PrecipitationChance
        {
            get
            {
                var precipChance = CurrentObject.daypart[0].precipChance[ItemIndex*2];

                //if precipchange is null and it is today
                if (precipChance == null && ItemIndex == 0)
                {
                    //return "--";

                    //return this evening's precip chance
                    return CurrentObject.daypart[0].precipChance[ItemIndex*2 + 1] + "%";
                }
                else
                {
                    return CurrentObject.daypart[0].precipChance[ItemIndex*2] + "%";
                }

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

        public string ShortDayName
        {
            get
            {
                if (ItemIndex == 0)
                {
                    var dayText = CurrentObject.daypart[0].daypartName[ItemIndex * 2];

                    return (string.IsNullOrEmpty(dayText) ? CurrentObject.daypart[0].daypartName[ItemIndex * 2 + 1] : dayText);
                }
                else
                {
                    //get windows current culture\language
                    var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];


                    var abbname =  new CultureInfo(language).DateTimeFormat.GetAbbreviatedDayName(CurrentObject.validTimeLocal[ItemIndex]
                        .DayOfWeek);

                        //VariousUtils.UppercaseFirst(CurrentObject.dayOfWeek[ItemIndex]);


                        //return short day name + day complete date
                    return VariousUtils.UppercaseFirst(abbname) + " " + CurrentObject.validTimeLocal[ItemIndex].Day;


                }
            }
        }


        public DayButtonAdapter(V3WxForecastDaily fcst, int itemIndex)
        {
            CurrentObject = fcst;
            ItemIndex = itemIndex;
        }
    }
}
