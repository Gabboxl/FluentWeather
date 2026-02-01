using Microsoft.Toolkit.Uwp.Notifications;

using Windows.UI.Notifications;

namespace FluentWeather.Services
{
    internal partial class ToastNotificationsService
    {
        public void ShowWeatherToastNotification()
        {
            var content = new ToastContent
            {
                // More about the Launch property at https://docs.microsoft.com/dotnet/api/microsoft.toolkit.uwp.notifications.toastcontent
                Launch = "ToastContentActivationParams",

                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = "Sample Toast Notification"
                            },

                            new AdaptiveText
                            {
                                 Text = @"Click OK to see how activation from a toast notification can be handled in the ToastNotificationService."
                            }
                        }
                    }
                },

                Actions = new ToastActionsCustom
                {
                    Buttons =
                    {
                        // More about Toast Buttons at https://docs.microsoft.com/dotnet/api/microsoft.toolkit.uwp.notifications.toastbutton
                        new ToastButton("OK", "ToastButtonActivationArguments")
                        {
                            ActivationType = ToastActivationType.Foreground
                        },

                        new ToastButtonDismiss("Cancel")
                    }
                }
            };

            var toast = new ToastNotification(content.GetXml())
            {
                Tag = "WeatherToastTag"
            };

            ShowToastNotification(toast);
        }
    }
}
