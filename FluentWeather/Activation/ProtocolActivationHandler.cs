using System;
using System.Threading.Tasks;
using FluentWeather.Helpers;
using FluentWeather.Services;
using FluentWeather.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;

namespace FluentWeather.Activation
{
    /// <summary>
    /// Handles links such as "fluentweather:switch?placeId=...".
    /// The tray icons program uses them to open the app at a saved location.
    /// Package.appxmanifest registers the link name.
    /// Only locations that are already saved are accepted. Any other link just opens the app.
    /// </summary>
    internal class ProtocolActivationHandler : ActivationHandler<ProtocolActivatedEventArgs>
    {
        public const string Scheme = "fluentweather";

        protected override bool CanHandleInternal(ProtocolActivatedEventArgs args)
        {
            return args.Uri != null && string.Equals(args.Uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase);
        }

        protected override async Task HandleInternalAsync(ProtocolActivatedEventArgs args)
        {
            var placeId = GetQueryValue(args.Uri, "placeId");
            if (string.IsNullOrEmpty(placeId) || !await SavedLocationsService.ContainsAsync(placeId))
                return;

            if (NavigationService.Frame?.Content == null)
            {
                // Cold start. The main page reads this setting when it loads.
                await ApplicationData.Current.LocalSettings.SaveAsync("lastPlaceId", placeId);
            }
            else
            {
                // The app is already open. Ask the main page to switch.
                AppViewModelHolder.GetViewModel().RequestSwitchLocation(placeId);
            }
        }

        private static string? GetQueryValue(Uri uri, string name)
        {
            if (string.IsNullOrEmpty(uri.Query))
                return null;

            try
            {
                foreach (var entry in new WwwFormUrlDecoder(uri.Query))
                {
                    if (string.Equals(entry.Name, name, StringComparison.Ordinal))
                        return entry.Value;
                }
            }
            catch (Exception)
            {
                // Malformed query. Treat it as a plain "open the app" link.
            }

            return null;
        }
    }
}
