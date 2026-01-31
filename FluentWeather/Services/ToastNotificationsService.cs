using System;
using System.Threading.Tasks;

using FluentWeather.Activation;

using Windows.ApplicationModel.Activation;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentWeather.Services
{
    internal partial class ToastNotificationsService : ActivationHandler<ToastNotificationActivatedEventArgs>
    {
        public void ShowToastNotification(ToastNotification toastNotification)
        {
            try
            {
                ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
            }
            catch (Exception)
            {
                // TODO: Adding ToastNotification can fail in rare conditions, please handle exceptions as appropriate to your scenario.
            }
        }

        protected override async Task HandleInternalAsync(ToastNotificationActivatedEventArgs args)
        {
            if (args.Argument == "ToastButtonActivationArguments")
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = "yo";
                dialog.PrimaryButtonText = "side";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
                dialog.Content = "xdxd";

                //dialog.FullSizeDesired = true;
                var result = await dialog.ShowAsync();
            }
            else
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = "yo";
                dialog.PrimaryButtonText = "side";
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
                dialog.Content = "xdxd";

                //dialog.FullSizeDesired = true;
                var result = await dialog.ShowAsync();
            }
            
            await Task.CompletedTask;
        }
    }
}
