using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FluentWeather.Services;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using FluentWeather.ViewModels;

namespace FluentWeather
{
    public sealed partial class App : Application
    {
        private AppViewModel AppViewModel = AppViewModelHolder.GetViewModel();

        private Lazy<ActivationService> _activationService;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }


        public IServiceProvider Container { get; }

        private IServiceProvider ConfigureDependencyInjection()
        {
            var serviceCollection = new ServiceCollection();


            return serviceCollection.BuildServiceProvider();
        }


        public App()
        {
            this.InitializeComponent();
            //this.Suspending += OnSuspending;
            Container = ConfigureDependencyInjection();

            UnhandledException += OnAppUnhandledException;

            bool isDebugMode = false;

#if DEBUG
            isDebugMode = true;
#endif

            if (!isDebugMode)
            {
                Microsoft.AppCenter.AppCenter.Start(
                    "test",
                    typeof(Microsoft.AppCenter.Analytics.Analytics),
                    typeof(Microsoft.AppCenter.Crashes.Crashes)
                );

                //Microsoft.AppCenter.Crashes.Crashes.SetEnabledAsync()

                //Microsoft.AppCenter.Analytics.Analytics.TrackEvent("App started");
            }

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);


            //set the global view model which we can use anytime for things
            this.AppViewModel = AppViewModelHolder.GetViewModel();
        }


        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

            if (e.PrelaunchActivated == false)
            {
                await ActivationService.ActivateAsync(e);
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new System.Exception("Failed to load Page " + e.SourcePageType.FullName);
        }


        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: salvare lo stato dell'applicazione e arrestare eventuali attività eseguite in background
            deferral.Complete();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private void OnAppUnhandledException(
            object sender,
            Windows.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            //http://blog.wpdev.fr/inspecting-unhandled-exceptions-youve-got-only-one-chance/
            Exception exceptionThatDoesntGoAway = e.Exception;

            //create a list of ErrorAttachmentLog
            var attachments = new List<ErrorAttachmentLog>();


            //Trace.WriteLine("SIDE2: " + exceptionThatDoesntGoAway.StackTrace);

            e.Handled = true;

            Crashes.TrackError(exceptionThatDoesntGoAway, attachments: attachments.ToArray());

            //visualizzare un dialog con si è verificato un errore
        }

        private ActivationService CreateActivationService()
        {
            //return new ActivationService(this, typeof(Views.MainPage), new Lazy<UIElement>(CreateShell));
            return new ActivationService(this, typeof(Views.MainPage), null);
        }

        /*private UIElement CreateShell()
        {
            return new Views.ShellPage();
        } */


        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }
    }
}
