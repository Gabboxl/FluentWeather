using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentWeather.Activation;
using FluentWeather.Core.Helpers;

using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentWeather.Services
{
    // For more information on understanding and extending activation flow see
    // https://github.com/microsoft/TemplateStudio/blob/main/docs/UWP/activation.md
    internal class ActivationService
    {
        private readonly App _app;
        private readonly Type _defaultNavItem;
        private Lazy<UIElement> _shell;

        private object _lastActivationArgs;

        public ActivationService(App app, Type defaultNavItem, Lazy<UIElement> shell = null)
        {
            _app = app;
            _shell = shell;
            _defaultNavItem = defaultNavItem;
        }

        public async Task ActivateAsync(object activationArgs)
        {
            if (IsInteractive(activationArgs))
            {
                // Initialize services that you need before app activation
                // take into account that the splash screen is shown while this code runs.
                await InitializeAsync();

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (Window.Current.Content == null)
                {
                    // Create a Shell or Frame to act as the navigation context
                    Window.Current.Content = _shell?.Value ?? new Frame();
                }
            }

            // Depending on activationArgs one of ActivationHandlers or DefaultActivationHandler
            // will navigate to the first page
            await HandleActivationAsync(activationArgs);
            _lastActivationArgs = activationArgs;

            if (IsInteractive(activationArgs))
            {
                // Ensure the current window is active
                Window.Current.Activate();

                // Tasks after activation
                await StartupAsync();
            }
        }

        private async Task InitializeAsync()
        {
            //TODO: reenable if livetile queue is needed
            //await Singleton<LiveTileService>.Instance.EnableQueueAsync().ConfigureAwait(false);
            await BackgroundTaskService.RegisterBackgroundTasksAsync().ConfigureAwait(false);

            await Singleton<AcrylicEffectsService>.Instance.InitializeAsync().ConfigureAwait(false);
            await Singleton<LiveTileService>.Instance.InitializeAsync().ConfigureAwait(false);
        }

        private async Task HandleActivationAsync(object activationArgs)
        {
            var activationhandlersList = GetActivationHandlers();

            //get the first activationhandler that can handle the activationargs
            var activationHandler = activationhandlersList.FirstOrDefault(h => h.CanHandle(activationArgs));

            //if there are activationhandlers that can handle the activationargs then handle it
            if (activationHandler != null)
            {
                await activationHandler.HandleAsync(activationArgs);
            }

            if (IsInteractive(activationArgs))
            {
                var defaultHandler = new DefaultActivationHandler(_defaultNavItem);
                if (defaultHandler.CanHandle(activationArgs))
                {
                    await defaultHandler.HandleAsync(activationArgs);
                }
            }
        }

        private async Task StartupAsync()
        {
            //always set the theme to dark
            await ThemeSelectorService.SetThemeAsync(ElementTheme.Dark);

            //TODO: reenable if theme selection is needed
            //await ThemeSelectorService.SetRequestedThemeAsync();

            await WhatsNewDisplayService.ShowIfAppropriateAsync();
            await FirstRunDisplayService.ShowIfAppropriateAsync();
            //Singleton<LiveTileService>.Instance.SampleUpdate();
        }

        private IEnumerable<ActivationHandler> GetActivationHandlers()
        {
            yield return Singleton<LiveTileService>.Instance;
            yield return Singleton<ToastNotificationsService>.Instance;
            yield return Singleton<BackgroundTaskService>.Instance;

            //da rimuovere se c'è altre instanze
            //yield break;
        }

        private bool IsInteractive(object args)
        {
            return args is IActivatedEventArgs;
        }
    }
}
