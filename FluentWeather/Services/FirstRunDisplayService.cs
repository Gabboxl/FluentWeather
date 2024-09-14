﻿using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using FluentWeather.Dialogs;
using FluentWeather.Helpers;
using System.Linq;
using Microsoft.Toolkit.Uwp.Helpers;

namespace FluentWeather.Services
{
    public static class FirstRunDisplayService
    {
        private static bool _shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            if (SystemInformation.Instance.IsFirstRun)
            {
                //set is24HourFormat settings based on system settings (values can be "24HourClock" or "12HourClock")
                string systemClockType = Windows.System.UserProfile.GlobalizationPreferences.Clocks.FirstOrDefault();

                await ApplicationData.Current.LocalSettings.SaveAsync("is12HourFormat",
                    systemClockType == "12HourClock");


                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, async () =>
                    {
                        if (!_shown)
                        {
                            _shown = true;
                            var dialog2 = new FirstRunDialog();
                            await dialog2.ShowAsync();
                        }
                    });
            }
        }
    }
}
