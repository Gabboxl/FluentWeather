using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace FluentWeather.Services
{
    /// <summary>
    /// The "Tray icons" setting. It is off by default.
    ///
    /// The tray icons program (FluentWeatherTray.exe in the FluentWeatherTray folder of the package)
    /// is the package's full trust process. When the setting is on, the app starts it, Windows starts
    /// it at sign-in through the "FluentWeatherTray" startup task, and the app writes the tray weather
    /// files (TrayWeatherFile). When the setting is off, the app writes "off" to tray-enabled.txt,
    /// the program sees that within 5 seconds and exits, and the startup task is turned off.
    /// </summary>
    internal static class TrayIconsService
    {
        private const string SettingKey = "trayIconsEnabled";
        private const string StartupTaskId = "FluentWeatherTray";
        private const string EnabledFileName = "tray-enabled.txt";

        /// <summary>Full trust processes exist on desktop Windows only.</summary>
        public static bool IsSupported =>
            ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1);

        public static bool IsEnabled
        {
            get
            {
                try
                {
                    return IsSupported && ApplicationData.Current.LocalSettings.Values[SettingKey] is true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>Starts the tray icons when the setting is on. Called when the app starts.</summary>
        public static async Task StartIfEnabledAsync()
        {
            if (IsEnabled)
                await LaunchAsync();
        }

        /// <summary>
        /// Turns the tray icons on or off and saves the choice. With "on", the caller writes the tray
        /// weather files and then calls StartIfEnabledAsync, so the icons show data at once.
        /// </summary>
        public static async Task SetEnabledAsync(bool enabled)
        {
            if (!IsSupported)
                return;

            ApplicationData.Current.LocalSettings.Values[SettingKey] = enabled;
            await WriteEnabledFileAsync(enabled);

            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);
                if (enabled)
                {
                    // The user can still turn this off in Task Manager or in Settings > Apps > Startup.
                    if (task.State == StartupTaskState.Disabled)
                        await task.RequestEnableAsync();
                }
                else
                {
                    task.Disable();
                }
            }
            catch (Exception)
            {
                // Without the startup task the icons still run until sign-out.
            }
        }

        private static async Task LaunchAsync()
        {
            try
            {
                // The program checks tray-enabled.txt at start. A second copy exits at once.
                await WriteEnabledFileAsync(true);
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            catch (Exception)
            {
                // The tray icons are optional. A failed start must never affect the app.
            }
        }

        private static async Task WriteEnabledFileAsync(bool enabled)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(EnabledFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, enabled ? "on" : "off");
            }
            catch (Exception)
            {
                // The program keeps its current state until the next write.
            }
        }
    }
}
