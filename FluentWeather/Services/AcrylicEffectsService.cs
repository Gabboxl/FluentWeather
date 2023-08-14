using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using FluentWeather.Helpers;

namespace FluentWeather.Services
{
    public sealed class AcrylicEffectsService : INotifyPropertyChanged
    {
        private const string SettingsKey = "effectsEnabled";

        private bool _effectsEnabled = true;

        public bool EffectsEnabled
        {
            get { return _effectsEnabled; }
            set { SetField(ref _effectsEnabled, value); }
        }

        private bool _useFallback = false;

        public bool UseFallback
        {
            get { return _useFallback; }
            set { SetField(ref _useFallback, value); }
        }

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


        private async Task<bool> LoadThemeFromSettingsAsync()
        {
            bool cacheValue = true;
            bool? settingsValue = await ApplicationData.Current.LocalSettings.ReadAsync<bool?>(SettingsKey);

            if (settingsValue != null)
            {
                cacheValue = settingsValue.Value;
            }

            return cacheValue;
        }

        private async Task SaveThemeInSettingsAsync(bool newValue)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(SettingsKey, newValue);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
