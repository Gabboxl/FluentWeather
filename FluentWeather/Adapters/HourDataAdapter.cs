using FluentWeather.Models;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using FluentWeather.Utils;

namespace FluentWeather.Adapters
{
    internal class HourDataAdapter(V3WxForecastHourly fcst, int itemIndex, WetUnits wetUnits)
    {
        public string Temperature
        {
            get { return fcst.temperature[itemIndex] + "°"; }
        }

        public string TemperatureFeelsLike
        {
            get { return fcst.temperatureFeelsLike[itemIndex] + "°" + MeasureUnitUtils.GetTemperatureUnits(wetUnits); }
        }

        public string WindSpeed
        {
            get { return fcst.windSpeed[itemIndex] + " " +
                         MeasureUnitUtils.GetWindSpeedUnits(wetUnits) + " " +
                         fcst.windDirectionCardinal[itemIndex] + " (" + fcst.windDirection[itemIndex] + "°)"; }
        }

        public string WindGust
        {
            get { return fcst.windGust[itemIndex] + " " + fcst.windDirection[itemIndex]; }
        }

        public string Humidity
        {
            get { return fcst.relativeHumidity[itemIndex] + "%"; }
        }

        public int UvIndex
        {
            get { return fcst.uvIndex[itemIndex]; }
        }

        public string PrecipitationType
        {
            get { return fcst.precipType[itemIndex]; } //rain, snow, precip
        }

        public string CloudCover
        {
            get { return fcst.cloudCover[itemIndex] + "%"; }
        }

        public string RainAmount
        {
            get
            {
                return fcst.qpf[itemIndex] + " " + MeasureUnitUtils.GetLiquidPrecipitationUnits(wetUnits);
            }
        }

        public SvgImageSource SvgImageIcon
        {
            get
            {
                return new SvgImageSource
                {
                    UriSource = new Uri("ms-appx:///Assets/weticons/" + fcst.iconCode[itemIndex] + ".svg")
                };
            }
        }

        public string Phrase
        {
            get { return fcst.wxPhraseLong[itemIndex]; }
        }

        public string PrecipitationChance
        {
            get { return fcst.precipChance[itemIndex] + "%"; }
        }

        public string Pressure
        {
            get { return fcst.pressureMeanSeaLevel[itemIndex] + " " + MeasureUnitUtils.GetPressureUnits(wetUnits); }
        }

        public SvgImageSource SvgPrecipIcon
        {
            get
            {
                //different icon name for different precipitation ranges


                return new SvgImageSource
                {
                    //todo: to use different drop icon based on precip quantity
                    UriSource = new Uri("ms-appx:///Assets/varicons/" + VariousUtils.GetPrecipIconChance(fcst.precipChance[itemIndex]) + ".svg")
                };
            }
        }

        public string Hour
        {
            get
            {
                //using task.run to avoid deadlock
                var result = Task.Run(() => VariousUtils.GetTimeBasedOnUserSettings(fcst.validTimeLocal[itemIndex].DateTime));
                return result.Result;
                
            }
        }
    }
}
