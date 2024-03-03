using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.UI.StartScreen;
using FluentWeather.Core.Helpers;
using FluentWeather.Services;

namespace FluentWeather.BackgroundTasks
{
    public sealed class LiveTileBackgroundTask : BackgroundTask
    {

        private volatile bool _cancelRequested = false;
        private IBackgroundTaskInstance _taskInstance;
        private BackgroundTaskDeferral _deferral;

        public override void Register()
        {
            var taskName = GetType().Name;
            var taskRegistration =
                BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == taskName).Value;

            if (taskRegistration == null)
            {
                var builder = new BackgroundTaskBuilder()
                {
                    Name = taskName
                };

                // TODO: Define the trigger for your background task and set any (optional) conditions
                // More details at https://docs.microsoft.com/windows/uwp/launch-resume/create-and-register-an-inproc-background-task
                builder.SetTrigger(new TimeTrigger(15, false));
                //builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.IsNetworkRequested = true;


                builder.Register();

                //update the tile for the first time
                Singleton<LiveTileService>.Instance.UpdateWeatherTileFull();
            }
        }

        public override Task RunAsyncInternal(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance == null)
            {
                return null;
            }

            _deferral = taskInstance.GetDeferral();

            return Task.Run(async () =>
            {
                //// TODO: Insert the code that should be executed in the background task here.
                //// This sample initializes a timer that counts to 100 in steps of 10.  It updates Message each time.

                //// Documentation:
                ////      * General: https://docs.microsoft.com/windows/uwp/launch-resume/support-your-app-with-background-tasks
                ////      * Debug: https://docs.microsoft.com/windows/uwp/launch-resume/debug-a-background-task
                ////      * Monitoring: https://docs.microsoft.com/windows/uwp/launch-resume/monitor-background-task-progress-and-completion

                //// To show the background progress and message on any page in the application,
                //// subscribe to the Progress and Completed events.
                //// You can do this via "BackgroundTaskService.GetBackgroundTasksRegistration"


                _taskInstance = taskInstance; //it can be removed as it isnt used anywhere


                //update livetile data

                Singleton<LiveTileService>.Instance.UpdateWeatherTileFull();

                //inform the system that the background task is completed
                _deferral.Complete();
            });
        }

        public override void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _cancelRequested = true;

            // TODO: Insert code to handle the cancelation request here.
            // Documentation: https://docs.microsoft.com/windows/uwp/launch-resume/handle-a-cancelled-background-task

        }


        private static async void UpdateTile()
        {
            AppListEntry entry = (await Package.Current.GetAppListEntriesAsync())[0];

// Check if Start supports your app
            //bool isSupported = StartScreenManager.GetDefault().SupportsAppListEntry(entry);


// Check if your app is currently pinned
            bool isPinned = await StartScreenManager.GetDefault().ContainsAppListEntryAsync(entry);
        }
    }
}
