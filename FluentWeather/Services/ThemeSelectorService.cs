using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using FluentWeather.Helpers;

namespace FluentWeather.Services
{
    public static class ThemeSelectorService
    {
        private const string SettingsKey = "AppBackgroundRequestedTheme";

        public static ElementTheme Theme { get; set; } = ElementTheme.Default;

        public static async Task InitializeAsync()
        {
            Theme = await LoadThemeFromSettingsAsync();
        }

        public static async Task SetThemeAsync(ElementTheme theme)
        {
            Theme = theme;

            await SetRequestedThemeAsync();
            await SaveThemeInSettingsAsync(Theme);
        }

        public static async Task SetRequestedThemeAsync()
        {
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Window.Current.Content is FrameworkElement frameworkElement)
                    {
                        frameworkElement.RequestedTheme = Theme;

                        bool IsDark = frameworkElement.ActualTheme == ElementTheme.Dark;
                        bool IsHighContrast = new AccessibilitySettings().HighContrast;

                        Color ForegroundColor = IsDark || IsHighContrast ? Colors.White : Colors.Black;
                        Color BackgroundColor = IsHighContrast ? Color.FromArgb(255, 0, 0, 0) : IsDark ? Color.FromArgb(255, 32, 32, 32) : Color.FromArgb(255, 243, 243, 243);

                        bool ExtendViewIntoTitleBar = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
                        ApplicationViewTitleBar TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                        TitleBar.ForegroundColor = TitleBar.ButtonForegroundColor = ForegroundColor;
                        TitleBar.BackgroundColor = TitleBar.InactiveBackgroundColor = BackgroundColor;
                        TitleBar.ButtonBackgroundColor = TitleBar.ButtonInactiveBackgroundColor = ExtendViewIntoTitleBar ? Colors.Transparent : BackgroundColor;

                    }
                });
            }
        }

        private static async Task<ElementTheme> LoadThemeFromSettingsAsync()
        {
            ElementTheme cacheTheme = ElementTheme.Dark;
            string themeName = await ApplicationData.Current.LocalSettings.ReadAsync<string>(SettingsKey);

            if (!string.IsNullOrEmpty(themeName))
            {
                Enum.TryParse(themeName, out cacheTheme);
            }

            return cacheTheme;
        }

        private static async Task SaveThemeInSettingsAsync(ElementTheme theme)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(SettingsKey, theme.ToString());
        }
    }
}
