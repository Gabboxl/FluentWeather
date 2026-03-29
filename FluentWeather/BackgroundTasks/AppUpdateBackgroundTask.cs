using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using FluentWeather.Services;
using FluentWeather.Utils;

namespace FluentWeather.BackgroundTasks
{
    public sealed class AppUpdateBackgroundTask : BackgroundTask
    {
        private volatile bool _cancelRequested;
        private BackgroundTaskDeferral _deferral;

        public override void Register()
        {
            const string taskName = nameof(AppUpdateBackgroundTask);
            var taskRegistration = BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == taskName).Value;

            if (taskRegistration != null)
            {
                //TODO: da rimuovere al prossimo aggiornamento dell'app
                taskRegistration.Unregister(true);
                //  return;
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = taskName
            };

            builder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
            builder.Register();
        }

        protected override Task RunAsyncInternal(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance == null)
                return Task.CompletedTask;

            _deferral = taskInstance.GetDeferral();

            return Task.Run(async () =>
            {
                foreach (var item in BackgroundTaskRegistration.AllTasks)
                {
                    item.Value.Unregister(true);
                }
                _deferral.Complete();
            });
        }

        protected override void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _cancelRequested = true;
        }
    }
}
