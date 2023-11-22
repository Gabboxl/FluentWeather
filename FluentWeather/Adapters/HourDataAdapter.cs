using FluentWeather.Models;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using FluentWeather.Utils;

namespace FluentWeather.Adapters
{
    internal class HourDataAdapter
    {
        private readonly V3WxForecastHourly CurrentObject;

        public readonly int ItemIndex;

        private WetUnits currentUnits;

        public string Temperature
        {
            get { return CurrentObject.temperature[ItemIndex] + "°"; }
        }

        public string TemperatureFeelsLike
        {
            get { return CurrentObject.temperatureFeelsLike[ItemIndex] + "°" + MeasureUnitUtils.GetTemperatureUnits(currentUnits); }
        }

        public string WindSpeed
        {
            get { return CurrentObject.windSpeed[ItemIndex] + " " +
                         MeasureUnitUtils.GetWindSpeedUnits(currentUnits) + " " +
                         CurrentObject.windDirectionCardinal[ItemIndex] + " (" + CurrentObject.windDirection[ItemIndex] + "°)"; }
        }

        public string WindGust
        {
            get { return CurrentObject.windGust[ItemIndex] + " " + CurrentObject.windDirection[ItemIndex]; }
        }

        public string Humidity
        {
            get { return CurrentObject.relativeHumidity[ItemIndex] + "%"; }
        }

        public int UvIndex
        {
            get { return CurrentObject.uvIndex[ItemIndex]; }
        }

        public string PrecipitationType
        {
            get { return CurrentObject.precipType[ItemIndex]; } //rain, snow, precip
        }

        public string CloudCover
        {
            get { return CurrentObject.cloudCover[ItemIndex] + "%"; }
        }

        public string RainAmount
        {
            get
            {
                return CurrentObject.qpf[ItemIndex] + " " + MeasureUnitUtils.GetLiquidPrecipitationUnits(currentUnits);
            }
        }

        public SvgImageSource SvgImageIcon
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
            get { return CurrentObject.wxPhraseLong[ItemIndex]; }
        }

        public string PrecipitationChance
        {
            get { return CurrentObject.precipChance[ItemIndex] + "%"; }
        }

        public SvgImageSource SvgPrecipIcon
        {
            get
            {
                return new SvgImageSource
                {
                    //todo: to use different drop icon based on precip quantity
                    UriSource = new Uri("ms-appx:///Assets/varicons/" + "blur" + ".svg")
                };
            }
        }

        public string Hour
        {
            get
            {
                //using task.run to avoid deadlock
                var result = Task.Run(() => VariousUtils.GetTimeBasedOnUserSettings(CurrentObject.validTimeLocal[ItemIndex].DateTime));
                return result.Result;
                
            }
        }


        public HourDataAdapter(V3WxForecastHourly fcst, int itemIndex, WetUnits wetUnits)
        {
            CurrentObject = fcst;
            ItemIndex = itemIndex;
            currentUnits = wetUnits;
        }
    }
}
