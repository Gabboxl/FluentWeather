using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentWeather.Activation;
using FluentWeather.BackgroundTasks;
using FluentWeather.Core.Helpers;

using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;

namespace FluentWeather.Services
{
    internal class BackgroundTaskService : ActivationHandler<BackgroundActivatedEventArgs>
    {
        private static IEnumerable<BackgroundTask> BackgroundTasks => BackgroundTaskInstances.Value;

        private static readonly Lazy<IEnumerable<BackgroundTask>> BackgroundTaskInstances = new(CreateInstances);

        public static async Task RegisterBackgroundTasksAsync()
        {
            BackgroundExecutionManager.RemoveAccess();
            var result = await BackgroundExecutionManager.RequestAccessAsync();

            if (result is BackgroundAccessStatus.DeniedBySystemPolicy or BackgroundAccessStatus.DeniedByUser)
                return;

            foreach (var task in BackgroundTasks)
            {
                task.Register();
            }
        }

        public static BackgroundTaskRegistration GetBackgroundTasksRegistration<T>()
            where T : BackgroundTask
        {
            if (BackgroundTaskRegistration.AllTasks.All(t => t.Value.Name != typeof(T).Name))
            {
                // This condition should not be met. If it is it means the background task was not registered correctly.
                // Please check CreateInstances to see if the background task was properly added to the BackgroundTasks property.
                return null;
            }

            return (BackgroundTaskRegistration)BackgroundTaskRegistration.AllTasks.FirstOrDefault(t => t.Value.Name == typeof(T).Name).Value;
        }

        private static void Start(IBackgroundTaskInstance taskInstance)
        {
            var task = BackgroundTasks.FirstOrDefault(b => b.Match(taskInstance?.Task?.Name));
            task?.RunAsync(taskInstance).FireAndForget();
        }

        protected override async Task HandleInternalAsync(BackgroundActivatedEventArgs args)
        {
            Start(args.TaskInstance);
            await Task.CompletedTask;
        }

        private static IEnumerable<BackgroundTask> CreateInstances()
        {
            var backgroundTasks = new List<BackgroundTask> {new LiveTileBackgroundTask()};
            return backgroundTasks;
        }
    }
}
