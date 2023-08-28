using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.StartScreen;
using Microsoft.Toolkit.Uwp.Notifications;
using FluentWeather.Core.Helpers;
using FluentWeather.Helpers;
using FluentWeather.Models;
using FluentWeather.Services;
using Newtonsoft.Json;
using FluentWeather.Utils;

namespace FluentWeather.BackgroundTasks
{
    public sealed class BackgroundTask1 : BackgroundTask
    {
        public static string Message { get; set; }

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

                builder.Register();
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


                new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.Reminder)
                    .AddArgument("action", "viewEvent")
                    .AddText("Test notification2")
                    .AddText("weather update")
                    //.AddText("10:00 AM - 10:30 AM")
                    .Show();


                _taskInstance = taskInstance; //it can be removed as it isnt used anywhere


                //update livetile data


                string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");

                string _systemLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

                HttpClient sharedClient2 = new()
                {
                    BaseAddress = new Uri("https://api.weather.com/v2/"),
                };


                if (lastPlaceId != null)
                {
                    var response = await sharedClient2.GetAsync(
                        "aggcommon/v3-wx-observations-current;v3-wx-forecast-hourly-10day;v3-wx-forecast-daily-10day;v3-location-point;v2idxDrySkinDaypart10;v2idxWateringDaypart10;v2idxPollenDaypart10;v2idxRunDaypart10;v2idxDriveDaypart10?format=json&placeid="
                        + lastPlaceId
                        + "&units=" + await VariousUtils.GetUnitsCode()
                        + "&language=" +
                        _systemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
                    //&locationType=city (x solo citta)

                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();


                    var newApiData = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);


                    Singleton<LiveTileService>.Instance.UpdateWeather(newApiData);
                }


                UpdateTile();
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
            bool isSupported = StartScreenManager.GetDefault().SupportsAppListEntry(entry);


// Check if your app is currently pinned
            bool isPinned = await StartScreenManager.GetDefault().ContainsAppListEntryAsync(entry);
        }
    }
}
