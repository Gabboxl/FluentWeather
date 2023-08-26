using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;
using Windows.UI.Core;
using Microsoft.Toolkit.Uwp.Notifications;

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
            var taskRegistration = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == taskName).Value;

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

            return Task.Run(() =>
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

                //we execute the code in the UI thread
                CoreApplication.MainView.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async () =>
                    {

                        new ToastContentBuilder()
                            .SetToastScenario(ToastScenario.Reminder)        
                            .AddArgument("action", "viewEvent")        
                            .AddArgument("eventId", 1983)        
                            .AddText("Adaptive Tiles Meeting")        
                            .AddText("Conf Room 2001 / Building 135")        
                            .AddText("10:00 AM - 10:30 AM")        
                            .AddComboBox("snoozeTime", "15", ("1", "1 minute"),        
                                ("15", "15 minutes"),        
                                ("60", "1 hour"),        
                                ("240", "4 hours"),        
                                ("1440", "1 day"))        
                            .AddButton(new ToastButton()
                                .SetSnoozeActivation("snoozeTime"))        
                            .AddButton(new ToastButton()
                                .SetDismissActivation())
                            .Show();

                    }
                );

                
                _taskInstance = taskInstance;
                ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(SampleTimerCallback), TimeSpan.FromSeconds(1));

            });
        }

        public override void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _cancelRequested = true;

           // TODO: Insert code to handle the cancelation request here.
           // Documentation: https://docs.microsoft.com/windows/uwp/launch-resume/handle-a-cancelled-background-task
        }

        private void SampleTimerCallback(ThreadPoolTimer timer)
        {
            if ((_cancelRequested == false) && (_taskInstance.Progress < 100))
            {
                _taskInstance.Progress += 10;
                Message = $"Background Task {_taskInstance.Task.Name} running";
            }
            else
            {
                timer.Cancel();

                if (_cancelRequested)
                {
                    Message = $"Background Task {_taskInstance.Task.Name} cancelled";
                }
                else
                {
                    Message = $"Background Task {_taskInstance.Task.Name} finished";
                }

                _deferral?.Complete();
            }
        }
    }
}
