﻿using System;
using System.Runtime.ExceptionServices;
using System.Security;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using FluentWeather.Services;
using Microsoft.Extensions.DependencyInjection;
using FluentWeather.ViewModels;
using Sentry;
using Sentry.Protocol;

namespace FluentWeather
{
    public sealed partial class App : Application
    {
        private AppViewModel _appViewModel = AppViewModelHolder.GetViewModel();

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
#if !DEBUG
                SentrySdk.Init(options =>
                {
                    // Tells which project in Sentry to send events to:
                    options.Dsn = "aka";
                    // When configuring for the first time, to see what the SDK is doing:
                    options.Debug = false;
                    // Enable Global Mode since this is a client app.
                    options.IsGlobalModeEnabled = true;
                    // TODO:Any other Sentry options you need go here.
                });
#endif

            UnhandledException += OnAppUnhandledException;

            InitializeComponent();
            Suspending += OnSuspending;
            Container = ConfigureDependencyInjection();

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);

            //set the global view model which we can use anytime for things
            _appViewModel = AppViewModelHolder.GetViewModel();
        }


        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (e.PrelaunchActivated == false)
            {
                await ActivationService.ActivateAsync(e);
            }
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: salvare lo stato dell'applicazione e arrestare eventuali attività eseguite in background
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
            deferral.Complete();
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void OnAppUnhandledException(
            object sender,
            Windows.UI.Xaml.UnhandledExceptionEventArgs e
        )
        {
            //http://blog.wpdev.fr/inspecting-unhandled-exceptions-youve-got-only-one-chance/
            Exception exceptionThatDoesntGoAway = e.Exception;

            if (exceptionThatDoesntGoAway != null)
            {
                // Tell Sentry this was an unhandled exception
                exceptionThatDoesntGoAway.Data[Mechanism.HandledKey] = false;
                exceptionThatDoesntGoAway.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
                // Capture the exception
                SentrySdk.CaptureException(exceptionThatDoesntGoAway);
                // Flush the event immediately
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }

            e.Handled = true;
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
