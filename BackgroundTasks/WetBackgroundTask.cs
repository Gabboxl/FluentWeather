using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundTasks
{
    public sealed partial class WetBackgroundTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();


            //XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
            //XmlNodeList textElements = toastXml.GetElementsByTagName("text");
            //textElements[0].AppendChild(toastXml.CreateTextNode("A toast example"));
            //textElements[1].AppendChild(toastXml.CreateTextNode("You've changed timezones!"));
            //ToastNotification notification = new(toastXml);
            //ToastNotificationManager.CreateToastNotifier().Show(notification);
            //Singleton<LiveTileService>.Instance.UpdateWeatherTileFull();

            // Inform the system that the task is finished.
            deferral.Complete();
        }
    }
}