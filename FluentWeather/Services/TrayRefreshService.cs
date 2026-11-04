using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace FluentWeather.Services
{
    /// <summary>
    /// The "FluentWeatherTrayRefresh" app service. The tray icons program calls it every 5 minutes
    /// while the app window is closed or minimized. Windows runs it inside the app without opening a
    /// window. It downloads the current conditions for the app's location and every saved location,
    /// and writes the tray files. Package.appxmanifest declares the app service.
    /// </summary>
    internal static class TrayRefreshService
    {
        public const string ServiceName = "FluentWeatherTrayRefresh";

        public static bool CanHandle(IBackgroundTaskInstance taskInstance)
        {
            return taskInstance?.TriggerDetails is AppServiceTriggerDetails details &&
                   string.Equals(details.Name, ServiceName, StringComparison.Ordinal);
        }

        public static void Handle(IBackgroundTaskInstance taskInstance)
        {
            var details = (AppServiceTriggerDetails) taskInstance.TriggerDetails;
            var deferral = taskInstance.GetDeferral();
            var completed = false;

            void Complete()
            {
                if (completed) return;
                completed = true;
                deferral.Complete();
            }

            taskInstance.Canceled += (_, _) => Complete();
            details.AppServiceConnection.ServiceClosed += (_, _) => Complete();
            details.AppServiceConnection.RequestReceived += async (_, args) =>
            {
                var messageDeferral = args.GetDeferral();
                var result = "ok";
                try
                {
                    await Task.WhenAll(TrayWeatherFile.RefreshCurrentAsync(), TrayWeatherFile.RefreshAllSavedAsync(true));
                }
                catch (Exception)
                {
                    result = "failed";
                }

                try
                {
                    await args.Request.SendResponseAsync(new ValueSet { ["result"] = result });
                }
                catch (Exception)
                {
                    // The tray closed the connection. Nothing else to do.
                }
                finally
                {
                    messageDeferral.Complete();
                    Complete();
                }
            };
        }
    }
}
