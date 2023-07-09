using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FluidWeather.Services;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using FluidWeather.ViewModels;

namespace FluidWeather
{
    public sealed partial class App : Application
    {
        private AppViewModel AppViewModel { get; }

        private Lazy<ActivationService> _activationService;

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }


        public IServiceProvider Container { get; }

        public IServiceProvider ConfigureDependencyInjection()
        {
            var serviceCollection = new ServiceCollection();


            return serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// Inizializza l'oggetto Application singleton. Si tratta della prima riga del codice creato
        /// creato e, come tale, corrisponde all'equivalente logico di main() o WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
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

                //Microsoft.AppCenter.Analytics.Analytics.TrackEvent("App started");
            }

            // Deferred execution until used. Check https://docs.microsoft.com/dotnet/api/system.lazy-1 for further info on Lazy<T> class.
            _activationService = new Lazy<ActivationService>(CreateActivationService);


            //set the global view model which we can use anytime for things
            this.AppViewModel = ViewModelHolder.GetViewModel();
        }

        /// <summary>
        /// Richiamato quando l'applicazione viene avviata normalmente dall'utente finale. All'avvio dell'applicazione
        /// verranno usati altri punti di ingresso per aprire un file specifico.
        /// </summary>
        /// <param name="e">Dettagli sulla richiesta e sul processo di avvio.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Non ripetere l'inizializzazione dell'applicazione se la finestra già dispone di contenuto,
            // assicurarsi solo che la finestra sia attiva
            if (rootFrame == null)
            {
                // Creare un frame che agisca da contesto di navigazione e passare alla prima pagina
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: caricare lo stato dall'applicazione sospesa in precedenza
                }

                // Posizionare il frame nella finestra corrente
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Quando lo stack di esplorazione non viene ripristinato, passare alla prima pagina
                    // configurando la nuova pagina per passare le informazioni richieste come parametro di
                    // navigazione
                    rootFrame.Navigate(typeof(Views.MainPage), e.Arguments);
                }

                // Assicurarsi che la finestra corrente sia attiva
                Window.Current.Activate();
            }

            if (!e.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(e);
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new System.Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Richiamato quando l'esecuzione dell'applicazione viene sospesa. Lo stato dell'applicazione viene salvato
        /// senza che sia noto se l'applicazione verrà terminata o ripresa con il contenuto
        /// della memoria ancora integro.
        /// </summary>
        /// <param name="sender">Origine della richiesta di sospensione.</param>
        /// <param name="e">Dettagli relativi alla richiesta di sospensione.</param>
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


            Trace.WriteLine("SIDE2: " + exceptionThatDoesntGoAway.StackTrace);

            e.Handled = true;

            Crashes.TrackError(exceptionThatDoesntGoAway, attachments: attachments.ToArray());

            //visualizzare un dialog con si è verificato un errore
        }

        private ActivationService CreateActivationService()
        {
            return new ActivationService(this, null, null);
        }

        /*private UIElement CreateShell()
        {
            return new Views.ShellPage();
        } */
    }
}
