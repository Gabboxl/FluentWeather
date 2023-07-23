using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using FluentWeather.Dialogs;
using FluentWeather.Helpers;

namespace FluentWeather.Services
{
    public static class FirstRunDisplayService
    {
        private static bool shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, async () =>
                {
                    string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");

                    if (lastPlaceId == null && !shown)
                    {
                        shown = true;
                        var dialog2 = new FirstRunDialog();
                        await dialog2.ShowAsync();
                    }
                });
        }
    }
}
