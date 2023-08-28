using System;
using System.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using FluentWeather.Models;
using FluentWeather.Utils;

namespace FluentWeather.Services
{
    internal partial class LiveTileService
    {
        public async void UpdateWeather(RootV3Response apiDataResponse)
        {
            var newIconUri = new Uri("ms-appx:///Assets/weticons/" + apiDataResponse.v3wxobservationscurrent.iconCode +
                                     ".svg");


            // These would be initialized with actual data
            string cityName = string.Join(", ",
                new[]
                {
                    apiDataResponse.v3locationpoint.LocationV3.displayName,
                    apiDataResponse.v3locationpoint.LocationV3.city,
                    apiDataResponse.v3locationpoint.LocationV3.country
                }.Where(s => !string.IsNullOrEmpty(s)).Distinct());


            string imageIconPath = "Assets/weticonspng/" + apiDataResponse.v3wxobservationscurrent.iconCode + ".png";


            WetUnits currentUnits = await VariousUtils.GetUnitsCode();

            string currentTemp = apiDataResponse.v3wxobservationscurrent.temperature + "°" +
                                 MeasureUnitUtils.GetTemperatureUnits(currentUnits);



            var builder = new TileContentBuilder();



            // Medium Tile built using only builder method.
            builder.AddTile(TileSize.Medium)
                .AddText(cityName, TileSize.Medium, hintStyle: AdaptiveTextStyle.Base)
                .AddAdaptiveTileVisualChild(MediumTileContent(imageIconPath, currentTemp), TileSize.Medium)
                .AddText(currentTemp, TileSize.Medium, hintStyle: AdaptiveTextStyle.Subtitle);

            builder.AddTile(Microsoft.Toolkit.Uwp.Notifications.TileSize.Wide)
                .AddText(cityName, TileSize.Wide)
                .AddAdaptiveTileVisualChild(WideTileContent(imageIconPath, currentTemp), TileSize.Wide);

            builder.AddTile(Microsoft.Toolkit.Uwp.Notifications.TileSize.Large)
                .AddText(cityName, TileSize.Large)
                .AddAdaptiveTileVisualChild(MediumTileContent(imageIconPath, currentTemp), TileSize.Large);


            // Large Tile using custom-made layout conjunction with builder helper method
           /* builder.AddTile(TileSize.Large)
                .AddAdaptiveTileVisualChild(CreateLargeTileLogoPayload(avatarLogoSource), TileSize.Large)
                .AddText("Hi,", TileSize.Large, hintAlign: AdaptiveTextAlign.Center, hintStyle: AdaptiveTextStyle.Title)
                .AddText(username, TileSize.Large, hintAlign: AdaptiveTextAlign.Center, hintStyle: AdaptiveTextStyle.SubtitleSubtle);

            */
            // Then create the tile notification
            var notification = new Windows.UI.Notifications.TileNotification(builder.Content.GetXml());
            UpdateTile(notification);
        }


        private ITileBindingContentAdaptiveChild MediumTileContent(string iconPath, string currentTemp)
        {
            return new AdaptiveGroup()
            {
                Children =
                {
                    new AdaptiveSubgroup()
                    {
                        HintWeight = 1
                    },


                    new AdaptiveSubgroup()
                    {
                        HintWeight = 3,

                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source = iconPath,
                                HintRemoveMargin = true,
                            },

                            new AdaptiveText()
                            {
                                Text = currentTemp,
                                HintStyle = AdaptiveTextStyle.Base,
                                HintAlign = AdaptiveTextAlign.Center,
                            },
                        }
                    },


                    new AdaptiveSubgroup()
                    {
                        HintWeight = 1
                    },




                }

            };
        }


        private ITileBindingContentAdaptiveChild WideTileContent(string iconPath, string currentTemp)
        {
            return new AdaptiveGroup()
            {
                Children =
                {
                    new AdaptiveSubgroup()
                    {
                        //HintWeight = 33,

                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source = iconPath

                            }
                        }
                    },

                    new AdaptiveSubgroup()
                    {
                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,

                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = currentTemp,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            },

                        }
                    }
                }
            };
        }



    }
}
