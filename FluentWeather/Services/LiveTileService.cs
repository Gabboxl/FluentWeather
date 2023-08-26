﻿using System;
using System.Linq;
using System.Threading.Tasks;

using FluentWeather.Activation;
using FluentWeather.Helpers;

using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace FluentWeather.Services
{
    internal partial class LiveTileService : ActivationHandler<LaunchActivatedEventArgs>
    {
        private const string QueueEnabledKey = "LiveTileNotificationQueueEnabled";

        public async Task EnableQueueAsync()
        {
            var queueEnabled = await ApplicationData.Current.LocalSettings.ReadAsync<bool>(QueueEnabledKey);
            if (!queueEnabled)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
                await ApplicationData.Current.LocalSettings.SaveAsync(QueueEnabledKey, true);
            }
        }

        public void UpdateTile(TileNotification notification)
        {
            try
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            }
            catch (Exception)
            {
                // TODO: Updating LiveTile can fail in rare conditions, please handle exceptions as appropriate to your scenario.
            }
        }

        public async Task<bool> PinSecondaryTileAsync(SecondaryTile tile, bool allowDuplicity = false)
        {
            try
            {
                if (!await IsAlreadyPinnedAsync(tile) || allowDuplicity)
                {
                    return await tile.RequestCreateAsync();
                }

                return false;
            }
            catch (Exception)
            {
                // TODO: Adding SecondaryTile can fail in rare conditions, please handle exceptions as appropriate to your scenario.
                return false;
            }
        }

        private async Task<bool> IsAlreadyPinnedAsync(SecondaryTile tile)
        {
            var secondaryTiles = await SecondaryTile.FindAllAsync();
            return secondaryTiles.Any(t => t.Arguments == tile.Arguments);
        }

        protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            // If app is launched from a SecondaryTile, tile arguments property is contained in args.Arguments
            // var secondaryTileArguments = args.Arguments;

            // If app is launched from a LiveTile notification update, TileContent arguments property is contained in args.TileActivatedInfo.RecentlyShownNotifications
            // var tileUpdatesArguments = args.TileActivatedInfo.RecentlyShownNotifications;
            await Task.CompletedTask;
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
        {
            return LaunchFromSecondaryTile(args) || LaunchFromLiveTileUpdate(args);
        }

        private bool LaunchFromSecondaryTile(LaunchActivatedEventArgs args)
        {
            // If app is launched from a SecondaryTile, tile arguments property is contained in args.Arguments
            // TODO: Implement your own logic to determine if you can handle the SecondaryTile activation
            return false;
        }

        private bool LaunchFromLiveTileUpdate(LaunchActivatedEventArgs args)
        {
            // If app is launched from a LiveTile notification update, TileContent arguments property is contained in args.TileActivatedInfo.RecentlyShownNotifications
            // TODO: Implement your own logic to determine if you can handle the LiveTile notification update activation
            return false;
        }
    }
}
