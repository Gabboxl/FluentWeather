using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using FluentWeather.Services;
using FluentWeather.Utils;

namespace FluentWeather.BackgroundTasks
{
    public sealed class LiveTileBackgroundTask : BackgroundTask
    {
        private volatile bool _cancelRequested;
        private BackgroundTaskDeferral _deferral;

        public override void Register()
        {
            const string taskName = nameof(LiveTileBackgroundTask);
            var taskRegistration = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == taskName).Value;

            if (taskRegistration != null)
                return;

            var builder = new BackgroundTaskBuilder
            {
                Name = taskName,
                IsNetworkRequested = true,
                CancelOnConditionLoss = true
            };

            builder.SetTrigger(new TimeTrigger(15, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            builder.Register();
        }

        protected override Task RunAsyncInternal(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance == null)
                return null;

            _deferral = taskInstance.GetDeferral();

            return Task.Run(async () =>
            {
                Singleton<LiveTileService>.Instance.UpdateWeatherTileFull();

                _deferral.Complete();
            });
        }

        protected override void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _cancelRequested = true;

            // TODO: Insert code to handle the cancelation request here.
            // Documentation: https://docs.microsoft.com/windows/uwp/launch-resume/handle-a-cancelled-background-task
        }
    }
}
