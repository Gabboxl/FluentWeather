﻿using System;
using System.Linq;
using Windows.Storage;
using FluentWeather.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using FluentWeather.Models;
using FluentWeather.Utils;
using Newtonsoft.Json;

namespace FluentWeather.Services
{
    public partial class LiveTileService
    {
        public async void UpdateWeatherFull()
        {
            string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");

            var response = await new ApiUtils().GetFullData(lastPlaceId);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var newApiData = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);

                UpdateWeatherMainTile(newApiData);

            }
        }

        public async void UpdateWeatherMainTile(RootV3Response apiDataResponse)
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
                .SetBackgroundImage( new Uri("ms-appx:///Assets/livetilesbgs/" + IconsDictionary.IconCodeToBackgroundImageNameDictionary[
                    apiDataResponse.v3wxobservationscurrent.iconCode.ToString()] + ".jpg"), TileSize.Medium)

                .AddText(cityName, TileSize.Medium, hintStyle: AdaptiveTextStyle.Base,
                    hintAlign: AdaptiveTextAlign.Center)
                .AddAdaptiveTileVisualChild(MediumTileContent(imageIconPath, currentTemp), TileSize.Medium)
                ;

            builder.AddTile(TileSize.Wide)
                .SetBackgroundImage( new Uri("ms-appx:///Assets/livetilesbgs/" + IconsDictionary.IconCodeToBackgroundImageNameDictionary[
                    apiDataResponse.v3wxobservationscurrent.iconCode.ToString()] + ".jpg"), TileSize.Wide)
                .AddText(cityName, TileSize.Wide, hintStyle: AdaptiveTextStyle.Base,
                    hintAlign: AdaptiveTextAlign.Center)
                .AddAdaptiveTileVisualChild(WideTileContent(imageIconPath, currentTemp), TileSize.Wide)
                .AddText(mainPhrase, TileSize.Wide, hintStyle: AdaptiveTextStyle.BaseSubtle,
                    hintAlign: AdaptiveTextAlign.Center);

            builder.AddTile(TileSize.Large)
                .SetBackgroundImage( new Uri("ms-appx:///Assets/livetilesbgs/" + IconsDictionary.IconCodeToBackgroundImageNameDictionary[
                    apiDataResponse.v3wxobservationscurrent.iconCode.ToString()] + ".jpg"), TileSize.Large)
                .AddText(cityName, TileSize.Large, hintStyle: AdaptiveTextStyle.Title,
                    hintAlign: AdaptiveTextAlign.Center)
                .AddAdaptiveTileVisualChild(LargeTileContent(imageIconPath, currentTemp), TileSize.Large)
                .AddText(mainPhrase, TileSize.Large, hintStyle: AdaptiveTextStyle.SubtitleSubtle,
                    hintAlign: AdaptiveTextAlign.Center);


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
                                Source = iconPath,
                                HintRemoveMargin = true,
                                HintAlign = AdaptiveImageAlign
                                    .Center //Images can be set to align left, center, or right using the hint-align attribute.
                                //---> This will also cause images to display at their native resolution instead of stretching to fill width.
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
                    new AdaptiveSubgroup() {HintWeight = 1},

                    new AdaptiveSubgroup()
                    {
                        HintWeight = 2,
                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source = iconPath,
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

                    new AdaptiveSubgroup() {HintWeight = 1},
                }
            };
        }


        private ITileBindingContentAdaptiveChild LargeTileContent(string iconPath, string currentTemp)
        {
            return new AdaptiveGroup()
            {
                Children =
                {
                    new AdaptiveSubgroup() {HintWeight = 1},

                    new AdaptiveSubgroup()
                    {
                        HintTextStacking = AdaptiveSubgroupTextStacking.Center,
                        HintWeight = 2,
                        Children =
                        {
                            new AdaptiveImage()
                            {
                                Source = iconPath,
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
                                HintStyle = AdaptiveTextStyle.Title
                            }
                        }
                    },

                    new AdaptiveSubgroup() {HintWeight = 1},
                }
            };
        }
    }
}
