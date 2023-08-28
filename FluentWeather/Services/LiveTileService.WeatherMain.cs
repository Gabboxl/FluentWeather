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

            string mainPhrase = apiDataResponse.v3wxobservationscurrent.cloudCoverPhrase;



            var builder = new TileContentBuilder();



            // Medium Tile built using only builder method.
            builder.AddTile(TileSize.Medium)
                .AddText(cityName, TileSize.Medium, hintStyle: AdaptiveTextStyle.Base, hintAlign: AdaptiveTextAlign.Center)
                .AddAdaptiveTileVisualChild(MediumTileContent(imageIconPath, currentTemp), TileSize.Medium)
                ;

            builder.AddTile(Microsoft.Toolkit.Uwp.Notifications.TileSize.Wide)
                .AddText(cityName, TileSize.Wide, hintStyle: AdaptiveTextStyle.Base, hintAlign: AdaptiveTextAlign.Center)
            .AddAdaptiveTileVisualChild(WideTileContent(imageIconPath, currentTemp), TileSize.Wide)
                .AddText(mainPhrase, TileSize.Wide, hintStyle: AdaptiveTextStyle.BaseSubtle, hintAlign: AdaptiveTextAlign.Center);

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
                        HintWeight = 1,
                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source =  iconPath,
                                HintRemoveMargin = true,
                                HintAlign = AdaptiveImageAlign.Center
                            },
                            new AdaptiveText()
                            {
                                Text = currentTemp,
                                HintAlign = AdaptiveTextAlign.Center,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            }
                        }
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
                    new AdaptiveSubgroup() { HintWeight = 1 },

                    new AdaptiveSubgroup()
                    {
                        HintWeight = 2,
                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source =  iconPath,
                                HintRemoveMargin = true,
                                HintAlign = AdaptiveImageAlign.Center
                            },
                        }
                    },

                    new AdaptiveSubgroup()
                    {
                        HintWeight = 2,
                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = currentTemp,
                                HintAlign = AdaptiveTextAlign.Center,
                                HintStyle = AdaptiveTextStyle.Subtitle
                            }
                        }
                    },

                    new AdaptiveSubgroup() { HintWeight = 1 },


                }

            };
        }



    }
}
