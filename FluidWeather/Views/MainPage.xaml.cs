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
using Windows.UI.Xaml.Media.Imaging;
using FluidWeather.Adapters;

namespace FluidWeather.Views
{
    public sealed partial class MainPage : Page
    {
        private AppViewModel AppViewModel { get; }

        public NavigationViewViewModel NavigationViewViewModel { get; } =
            new NavigationViewViewModel();

        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/v3/"),
        };


        public MainPage()
        {
            this.InitializeComponent();

            this.DataContext = this; //DataContext = ViewModel;
            Initialize();

            this.AppViewModel = ViewModelHolder.GetViewModel();



            //set parallaxview image
            var image = new Image();

            var bitmap = new BitmapImage();
            bitmap.UriSource = new Uri("ms-appx:///Assets/bgs/1.jpg");

            //darken bitmap image without effects


            image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
            image.Source = bitmap;




            parallaxView.Child = image;


        }

        private void Initialize()
        {
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

                var response = await sharedClient.GetAsync("location/searchflat?query=" + sender.Text + "&language=" + language + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230&format=json");
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

        private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

            var response = await sharedClient.GetAsync("aggcommon/v3-wx-observations-current;v3-wx-forecast-daily-10day;v3-location-point?format=json&placeid=" + ((SearchedLocation)args.SelectedItem).placeId + "&units=m&language=" +
                                                       language + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
            //&locationType=city (x solo citta)

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();


            var myDeserializedClass = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);

            updateUi(myDeserializedClass);
        }


        private void updateUi(RootV3Response myDeserializedClass)
        {
            //imagesource class creation

            var asd = new Uri("ms-appx:///Assets/weticons/" + myDeserializedClass.v3wxobservationscurrent.iconCode + ".svg");

            //imagesource
            //var imageSource = new SvgImageSource(asd);


            //get svgimagesource object from image source
            var svgImageSource = (SvgImageSource)mainIcon.Source;

            svgImageSource.UriSource = asd;

            mainIcon.Source = svgImageSource;


            //update chips


            PlaceText.Text = myDeserializedClass.v3locationpoint.LocationV3.displayName + ", " + myDeserializedClass.v3locationpoint.LocationV3.adminDistrict + ", " + myDeserializedClass.v3locationpoint.LocationV3.country;

            MainPhraseText.Text = myDeserializedClass.v3wxobservationscurrent.cloudCoverPhrase;

            CurrentTempText.Text = myDeserializedClass.v3wxobservationscurrent.temperature + "°C";

            FeelsLikeText.Text = "Feels like " + myDeserializedClass.v3wxobservationscurrent.temperatureFeelsLike + "°C";

            WindPanel.Value = myDeserializedClass.v3wxobservationscurrent.windSpeed + " km/h" + " " + myDeserializedClass.v3wxobservationscurrent.windDirectionCardinal;

            HumidityPanel.Value = myDeserializedClass.v3wxobservationscurrent.relativeHumidity + "%";

            PressurePanel.Value = myDeserializedClass.v3wxobservationscurrent.pressureMeanSeaLevel + " hPa";

            VisibilityPanel.Value = myDeserializedClass.v3wxobservationscurrent.visibility + " km";

            DewPointPanel.Value = myDeserializedClass.v3wxobservationscurrent.temperatureDewPoint + "°C";

            UVIndexPanel.Value = myDeserializedClass.v3wxobservationscurrent.uvIndex + " (" + myDeserializedClass.v3wxobservationscurrent.uvDescription + ")";


            int numdays = myDeserializedClass.v3wxforecastdaily10day.dayOfWeek.Count;

            //update days repeater itemssource with creating for every day a new DayButtonAdapter class
            List<DayButtonAdapter> dayButtonAdapters = new List<DayButtonAdapter>();

            int i = 0;

            foreach (var day in myDeserializedClass.v3wxforecastdaily10day.dayOfWeek)
            {
                dayButtonAdapters.Add(new DayButtonAdapter(myDeserializedClass.v3wxforecastdaily10day, i));

                i++;
            }

            repeaterDays.ItemsSource = dayButtonAdapters;



        }


        private Button _lastSelectedDayButton;

        private void DayButtonClick(object sender, RoutedEventArgs e)
        {
            // Reset the style of the previously selected button
            if (_lastSelectedDayButton != null)
            {
                //apply default button style of winui
                _lastSelectedDayButton.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];

            }

            // Apply the style to the selected button
            var button = (Button)sender;
            button.Style = (Style)Resources["ButtonStyle1"];

            // Keep track of the selected button
            _lastSelectedDayButton = button;



            
        }


        private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            //update ui
        }
    }
}
