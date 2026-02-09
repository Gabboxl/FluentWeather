using CommunityToolkit.Mvvm.ComponentModel;
using FluentWeather.Helpers;
using System.Threading.Tasks;
using Windows.Storage;
using WinRT;

namespace FluentWeather.Services
{
    [GeneratedBindableCustomPropertyAttribute]
    public sealed partial class AcrylicEffectsService : ObservableObject
    {
        private const string SettingsKey = "effectsEnabled";

        [ObservableProperty]
        public partial bool EffectsEnabled { get; set; } = true;

        [ObservableProperty]
        public partial bool UseFallback { get; set; }

        public async Task InitializeAsync()
        {
            EffectsEnabled = await LoadThemeFromSettingsAsync();
            UseFallback = !EffectsEnabled;
        }

        public async Task SetThemeAsync(bool newValue)
        {
            EffectsEnabled = newValue;
            UseFallback = !newValue;

            await SaveThemeInSettingsAsync(newValue);
        }

        private static async Task<bool> LoadThemeFromSettingsAsync()
        {
            bool cacheValue = true;
            bool? settingsValue = await ApplicationData.Current.LocalSettings.ReadAsync<bool?>(SettingsKey);

            if (settingsValue != null)
            {
                cacheValue = settingsValue.Value;
            }

            return cacheValue;
        }

        private static async Task SaveThemeInSettingsAsync(bool newValue)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(SettingsKey, newValue);
        }

    }
}
