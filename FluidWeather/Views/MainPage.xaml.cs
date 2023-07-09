using System;
using FluidWeather.Services;
using FluidWeather.ViewModels;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;
using FluidWeather.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using FluidWeather.Adapters;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using FluidWeather.Helpers;

namespace FluidWeather.Views
{
    public sealed partial class MainPage : Page
    {

        public MainPageViewModel MainPageViewModel { get; } =
            new MainPageViewModel();

        public NavigationViewViewModel NavigationViewViewModel { get; } =
            new NavigationViewViewModel();

        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/v3/"),
        };


        public MainPage()
        {
            //use the EntranceNavigationTransition when showing the page at startup (pageload)
            this.Transitions = new TransitionCollection();
            this.Transitions.Add(new EntranceThemeTransition());


            this.InitializeComponent();

            this.DataContext = this; //DataContext = ViewModel;
            Initialize();


            Task.Run(LoadApiData);
        }

        private void Initialize()
        {
            //set parallaxview image
            var image = new Image();

            var bitmap = new BitmapImage();
            bitmap.UriSource = new Uri("ms-appx:///Assets/bgs/1.jpg");

            image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
            image.Source = bitmap;

            parallaxView.Child = image;

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            AppTitleTextBlock.Text = "" + AppInfo.Current.DisplayInfo.DisplayName;
            Window.Current.SetTitleBar(AppTitleBar);

            //remove the solid-colored backgrounds behind the caption controls and system back button
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            //NavigationViewViewModel.Initialize(contentFrame, navigationView, KeyboardAccelerators);

            //pagina di default
            //NavigationService.Navigate(typeof(Views.DashWet));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput
                && !string.IsNullOrEmpty(sender.Text))
            {
                var finalitems = new List<SearchedLocation>();

                //get language code from system (like en-us)
                var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

                var response = await sharedClient.GetAsync("location/searchflat?query=" + sender.Text + "&language=" +
                                                           language +
                                                           "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230&format=json");
                //&locationType=city (x solo citta)

                //response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"{jsonResponse}\n");


                var myDeserializedClass = JsonConvert.DeserializeObject<SearchLocationResponse>(jsonResponse);

                foreach (var location in myDeserializedClass.location)
                {
                    Debug.WriteLine(location.address);
                }


                finalitems = myDeserializedClass.location.Select(x => x).ToList();
                // the select statement above is the same as the foreach below


                sender.ItemsSource = finalitems;
            }
        }

        private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args)
        {

            var selectedPlaceId = ((SearchedLocation) args.SelectedItem).placeId;


            //save location to settings
            await ApplicationData.Current.LocalSettings.SaveAsync("lastPlaceId", selectedPlaceId);


            await Task.Run(LoadApiData);
        }


        private async Task LoadApiData()
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () => { MainPageViewModel.IsLoadingData = true; }
            );

            string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");

            if (lastPlaceId != null)
            {
                var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

                var response = await sharedClient.GetAsync(
                    "aggcommon/v3-wx-observations-current;v3-wx-forecast-daily-10day;v3-location-point?format=json&placeid=" +
                    lastPlaceId
                    + "&units=m&language=" +
                    language + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
                //&locationType=city (x solo citta)

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();


                var myDeserializedClass = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);


                //we execute the code in the UI thread
                await CoreApplication.MainView.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        UpdateUi(myDeserializedClass);
                    }
                );

            }


            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () => { MainPageViewModel.IsLoadingData = false; }
            );
        }


        private void UpdateUi(RootV3Response rootV3Response)
        {
            //imagesource class creation

            var asd = new Uri("ms-appx:///Assets/weticons/" + rootV3Response.v3wxobservationscurrent.iconCode +
                              ".svg");

            //imagesource
            //var imageSource = new SvgImageSource(asd);


            //get svgimagesource object from image source
            var svgImageSource = (SvgImageSource) mainIcon.Source;

            svgImageSource.UriSource = asd;

            mainIcon.Source = svgImageSource;


            //update chips


            PlaceText.Text = rootV3Response.v3locationpoint.LocationV3.displayName + ", " +
                             rootV3Response.v3locationpoint.LocationV3.adminDistrict + ", " +
                             rootV3Response.v3locationpoint.LocationV3.country;

            MainPhraseText.Text = rootV3Response.v3wxobservationscurrent.cloudCoverPhrase;

            CurrentTempText.Text = rootV3Response.v3wxobservationscurrent.temperature + "°C";

            FeelsLikeText.Text =
                "Feels like " + rootV3Response.v3wxobservationscurrent.temperatureFeelsLike + "°C";

            WindPanel.Value = rootV3Response.v3wxobservationscurrent.windSpeed + " km/h" + " " +
                              rootV3Response.v3wxobservationscurrent.windDirectionCardinal;

            HumidityPanel.Value = rootV3Response.v3wxobservationscurrent.relativeHumidity + "%";

            PressurePanel.Value = rootV3Response.v3wxobservationscurrent.pressureMeanSeaLevel + " hPa";

            VisibilityPanel.Value = rootV3Response.v3wxobservationscurrent.visibility + " km";

            DewPointPanel.Value = rootV3Response.v3wxobservationscurrent.temperatureDewPoint + "°C";

            UVIndexPanel.Value = rootV3Response.v3wxobservationscurrent.uvIndex + " (" +
                                 rootV3Response.v3wxobservationscurrent.uvDescription + ")";


            //update days repeater itemssource with creating for every day a new DayButtonAdapter class

            int numdays = rootV3Response.v3wxforecastdaily10day.dayOfWeek.Count;


            List<DayButtonAdapter> dayButtonAdapters = new List<DayButtonAdapter>();

            int i = 0;

            foreach (var day in rootV3Response.v3wxforecastdaily10day.dayOfWeek)
            {
                dayButtonAdapters.Add(new DayButtonAdapter(rootV3Response.v3wxforecastdaily10day, i));

                i++;
            }


            var element = repeaterDays;
            var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;
            var animation = compositor.CreateScopedBatch(Windows.UI.Composition.CompositionBatchTypes.Animation);
            animation.Completed += (s, e) => { animation.Dispose(); };
            var slideAnimation = compositor.CreateScalarKeyFrameAnimation();
            slideAnimation.InsertKeyFrame(0f, 1000.0f);
            slideAnimation.InsertKeyFrame(1.0f, 0.0f);
            slideAnimation.Duration = TimeSpan.FromMilliseconds(600);

            //var animationGroup = compositor.CreateAnimationGroup();
            //animationGroup.Add(slideAnimation);

            var elementVisual = ElementCompositionPreview.GetElementVisual(element);
            elementVisual.StartAnimation(
                "Offset.x",
                slideAnimation
            );


            repeaterDays.ItemsSource = dayButtonAdapters;
        }


        private Button _lastSelectedDayButton;

        private void DayButtonClick(object sender, RoutedEventArgs e)
        {
            // Reset the style of the previously selected button
            if (_lastSelectedDayButton != null)
            {
                //apply default button style of winui
                _lastSelectedDayButton.Style = (Style) Application.Current.Resources["DefaultButtonStyle"];
            }

            // Apply the style to the selected button
            var button = (Button) sender;
            button.Style = (Style) Resources["ButtonStyle1"];

            // Keep track of the selected button
            _lastSelectedDayButton = button;
        }


        private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            //update ui

            //show entrance animation effect when reloading the page
            //var entranceAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("EntranceAnimation", mainIcon);
            //entranceAnimation.Configuration = new DirectConnectedAnimationConfiguration();
        }
    }
}
