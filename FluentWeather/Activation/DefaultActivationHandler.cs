using System;
using System.Threading.Tasks;
using FluentWeather.Services;
using Windows.ApplicationModel.Activation;

namespace FluentWeather.Activation
{
    internal class DefaultActivationHandler(Type navElement) : ActivationHandler<IActivatedEventArgs>
    {
        protected override async Task HandleInternalAsync(IActivatedEventArgs args)
        {
            // When the navigation stack isn't restored, navigate to the first page and configure
            // the new page by passing required information in the navigation parameter
            object arguments = null;
            if (args is LaunchActivatedEventArgs launchArgs)
            {
                arguments = launchArgs.Arguments;
            }

            NavigationService.Navigate(navElement, arguments);

            // TODO: Remove or change this sample which shows a toast notification when the app is launched.
            // You can use this sample to create toast notifications where needed in your app.
            //Singleton<ToastNotificationsService>.Instance.ShowToastNotificationSample();

            //TODO: esegui altro codice all'avvio dell'app qui

            await Task.CompletedTask;
        }

        protected override bool CanHandleInternal(IActivatedEventArgs args)
        {
            // None of the ActivationHandlers has handled the app activation
            return NavigationService.Frame.Content == null && navElement != null;
        }
    }
}
