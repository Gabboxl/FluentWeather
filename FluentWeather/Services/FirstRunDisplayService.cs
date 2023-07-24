using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
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
