using System.Threading.Tasks;
using Windows.Storage;
using FluentWeather.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentWeather.Services
{
    public sealed partial class AcrylicEffectsService : ObservableObject
    {
        private const string SettingsKey = "effectsEnabled";

        [ObservableProperty]
        private bool _effectsEnabled = true;

        [ObservableProperty]
        private bool _useFallback;

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
