using System;
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
using FluidWeather.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using FluidWeather.Adapters;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using CommunityToolkit.Labs.WinUI;
using FluidWeather.Helpers;
using FluidWeather.Utils;

namespace FluidWeather.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel MainPageViewModel { get; } =
            new MainPageViewModel();

        public NavigationViewViewModel NavigationViewViewModel { get; } =
            new NavigationViewViewModel();

        private AppViewModel AppViewModel = AppViewModelHolder.GetViewModel();

        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/v3/"),
        };

        private readonly string systemLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

        private Button _lastSelectedDayButton;


        //segemented settings units selectedindex
        public int SettingsUnitsSelectedIndex
        {
            get { return ApplicationData.Current.LocalSettings.ReadAsync<int>("selectedUnits").Result; }
        }

        //dictionary for every icon code and its corresponsing background image
        private static Dictionary<string, string> iconCodeToBackgroundImageNameDictionary = new()
        {
            {"0", "2"},
            {"1", "2"},
            {"2", "2"},
            {"3", "3"},
            {"4", "3"},
            {"5", "7"},
            {"6", "4"},
            {"7", "7"},
            {"8", "4"},
            {"9", "4"},
            {"10", "4"},
            {"11", "4"},
            {"12", "4"},
            {"13", "7"},
            {"14", "7"},
            {"15", "7"},
            {"16", "7"},
            {"17", "7"},
            {"18", "4"},
            {"19", "6"},
            {"20", "2"},
            {"21", "2"},
            {"22", "2"},
            {"23", "6"},
            {"24", "6"},
            {"25", "7"},
            {"26", "5"},
            {"27", "8"},
            {"28", "5"},
            {"29", "8"},
            {"30", "5"},
            {"31", "1"},
            {"32", "9"},
            {"33", "1"},
            {"34", "9"},
            {"35", "4"},
            {"36", "9"},
            {"37", "10"},
            {"38", "3"},
            {"39", "4"},
            {"40", "4"},
            {"41", "7"},
            {"42", "7"},
            {"43", "7"},
            {"44", "9"},
            {"45", "4"},
            {"46", "7"},
            {"47", "3"},
        };


        public MainPage()
        {
            //use the EntranceNavigationTransition when showing the page at startup (pageload)
            this.Transitions = new TransitionCollection
            {
                new EntranceThemeTransition()
            };


            this.InitializeComponent();

            this.DataContext = this; //DataContext = ViewModel;
            Initialize();

            InitializeStoryboards();

            AppViewModel.UpdateUIAction += (() => { Task.Run(LoadApiData); });


            Task.Run(LoadApiData);
        }

        private void Initialize()
        {

            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            AppTitleTextBlock.Text = "" + AppInfo.Current.DisplayInfo.DisplayName;
            Window.Current.SetTitleBar(TitleBarGrid);

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

                var response = await sharedClient.GetAsync("location/searchflat?query=" + sender.Text + "&language=" +
                                                           systemLanguage +
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


                List<SearchedLocation> finalitems = myDeserializedClass.location.Select(x => x).ToList();
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

        private async Task<WetUnits> GetUnitsCode()
        {
            int units = await ApplicationData.Current.LocalSettings.ReadAsync<int>("selectedUnits");

            //get unit code from enum
            WetUnits unitCode = (WetUnits) units;

            return unitCode;
        }

        private async Task LoadApiData()
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => { MainPageViewModel.IsLoadingData = true; }
            );

            string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");


            if (lastPlaceId != null)
            {
                var response = await sharedClient.GetAsync(
                    "aggcommon/v3-wx-observations-current;v3-wx-forecast-daily-10day;v3-location-point?format=json&placeid="
                    + lastPlaceId
                    + "&units=" + await GetUnitsCode()
                    + "&language=" +
                    systemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
                //&locationType=city (x solo citta)

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();


                var myDeserializedClass = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);


                //we execute the code in the UI thread
                await CoreApplication.MainView.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async () => { UpdateUi(myDeserializedClass); }
                );
            }


            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () => { MainPageViewModel.IsLoadingData = false; }
            );
        }


        /// <summary>
        /// storyboards code
        /// </summary>
        private Storyboard _storyboard1;
        private Storyboard _storyboard2;
        private bool _isImage1Active;

        private void CrossfadeToImage(Uri newImageUri)
        {
            BitmapImage newImage = new BitmapImage(newImageUri);

            if (_isImage1Active)
            {
                Image2.Source = newImage;
            }
            else
            {
                Image1.Source = newImage;
            }

            //remeber that the imageopened event is fired only when the image is shown in some way :(


            newImage.ImageOpened += (sender, args) =>
            {
                if (_isImage1Active)
                {
                    // Reset and start the storyboard
                    _storyboard1.Stop();
                    _storyboard2.Begin();
                }
                else
                {
                    // Reset and start the storyboard
                    _storyboard2.Stop();
                    _storyboard1.Begin();
                }

                _isImage1Active = !_isImage1Active;
            };
        }


        private void InitializeStoryboards()
        {
            _storyboard1 = CreateCrossfadeStoryboard(Image1, Image2);
            _storyboard2 = CreateCrossfadeStoryboard(Image2, Image1);

            _isImage1Active = true;
        }

        private Storyboard CreateCrossfadeStoryboard(Image fadeInImage, Image fadeOutImage)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fadeInAnimation, fadeInImage);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fadeOutAnimation, fadeOutImage);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);

            return storyboard;
        }


        /// <summary>
        /// update ui based on the api response
        /// </summary>
        /// <param name="rootV3Response"></param>
        private async void UpdateUi(RootV3Response rootV3Response)
        {
            var newImageUri = new Uri("ms-appx:///Assets/bgs/" +
                                      iconCodeToBackgroundImageNameDictionary[
                                          rootV3Response.v3wxobservationscurrent.iconCode.ToString()] + ".jpg");

            CrossfadeToImage(newImageUri);


            //imagesource class creation for weather icon

            var asd = new Uri("ms-appx:///Assets/weticons/" + rootV3Response.v3wxobservationscurrent.iconCode +
                              ".svg");

            //imagesource
            //var imageSource = new SvgImageSource(asd);

            //get svgimagesource object from image source
            var svgImageSource = (SvgImageSource) mainIcon.Source;
            svgImageSource.UriSource = asd;
            mainIcon.Source = svgImageSource;


            //update chips
            WetUnits currentUnits =
                (WetUnits) await ApplicationData.Current.LocalSettings.ReadAsync<int>("selectedUnits");


            UpdatedOnText.Text = "Updated on " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");


            PlaceText.Text = rootV3Response.v3locationpoint.LocationV3.displayName + ", " +
                             rootV3Response.v3locationpoint.LocationV3.adminDistrict + ", " +
                             rootV3Response.v3locationpoint.LocationV3.country;

            MainPhraseText.Text = rootV3Response.v3wxobservationscurrent.cloudCoverPhrase;

            CurrentTempText.Text = rootV3Response.v3wxobservationscurrent.temperature + "°" +
                                   MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            FeelsLikeText.Text =
                "Feels like " + rootV3Response.v3wxobservationscurrent.temperatureFeelsLike + "°" +
                MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            WindPanel.Value = rootV3Response.v3wxobservationscurrent.windSpeed + " " +
                              MeasureUnitUtils.GetWindSpeedUnits(currentUnits) + " " +
                              rootV3Response.v3wxobservationscurrent.windDirectionCardinal;

            HumidityPanel.Value = rootV3Response.v3wxobservationscurrent.relativeHumidity + "%";

            PressurePanel.Value = rootV3Response.v3wxobservationscurrent.pressureMeanSeaLevel + " " +
                                  MeasureUnitUtils.GetPressureUnits(currentUnits);

            //visibility with units based on the unit of measure saved in the settings
            VisibilityPanel.Value = rootV3Response.v3wxobservationscurrent.visibility + " " +
                                    MeasureUnitUtils.GetVisibilityUnits(currentUnits);

            DewPointPanel.Value = rootV3Response.v3wxobservationscurrent.temperatureDewPoint + "°" +
                                  MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            UVIndexPanel.Value = rootV3Response.v3wxobservationscurrent.uvIndex + " (" +
                                 rootV3Response.v3wxobservationscurrent.uvDescription + ")";


            //update days repeater itemssource with creating for every day a new DayButtonAdapter class

            List<DayButtonAdapter> dayButtonAdapters = new List<DayButtonAdapter>();

            int i = 0;

            foreach (var day in rootV3Response.v3wxforecastdaily10day.dayOfWeek)
            {
                dayButtonAdapters.Add(new DayButtonAdapter(rootV3Response.v3wxforecastdaily10day, i));

                i++;
            }


            /*var element = repeaterDays;
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


            elementVisual.StartAnimation("Offset.x", slideAnimation);*/


            repeaterDays.ItemsSource = dayButtonAdapters;

            //select first day button
            EmulateDayButtonClick(0);
        }

        private async void LoadHourlyData(DateTimeOffset dayToLoad)
        {
            string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");

            var response = await sharedClient.GetAsync(
                "aggcommon/v3-wx-forecast-hourly-10day?format=json&placeid=" +
                lastPlaceId
                + "&units=" + await GetUnitsCode() +
                "&language=" +
                systemLanguage + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");


            var jsonResponse = await response.Content.ReadAsStringAsync();

            var deserializedResponse = JsonConvert.DeserializeObject<RootStandaloneHourlyResponse>(jsonResponse);


            //update the ui
            List<HourDataAdapter> hourlyDataAdapters = new List<HourDataAdapter>();

            int i = 0;


            var firstDate = deserializedResponse.v3wxforecasthourly10day.validTimeLocal[0];

            //subtract daytoload to firstdate to get the index of the first day to load
            int daysDiff = dayToLoad.Date.Subtract(firstDate.Date).Days;

            if (daysDiff == -1) //this means that we are still in a tonight state and we add hours only until 7AM
            {
                for (int j = 0; j < 7; j++) //check only the first 7 hours of the day
                {
                    if (deserializedResponse.v3wxforecasthourly10day.validTimeLocal[j].Hour < 7)
                    {
                        hourlyDataAdapters.Add(new HourDataAdapter(deserializedResponse.v3wxforecasthourly10day, j));
                    }
                }
            }
            else
            {
                foreach (var date in deserializedResponse.v3wxforecasthourly10day.validTimeLocal)
                {
                    //the .Date property is used to compare only the date part of the datetime without the time (no hours, minutes, seconds)
                    if (date.Date == firstDate.Date.AddDays(daysDiff))
                    {
                        hourlyDataAdapters.Add(new HourDataAdapter(deserializedResponse.v3wxforecasthourly10day, i));
                    }

                    i++;
                }
            }

            hourlyListview.ItemsSource = hourlyDataAdapters;
        }


        private void DayButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;

            SetDayButtonClickedStyle(button);

            //get button's DayButtonAdapter object
            var dayButtonAdapter = (DayButtonAdapter) button.DataContext;

            //load hourly data
            LoadHourlyData(dayButtonAdapter.CurrentObject.validTimeLocal[dayButtonAdapter.ItemIndex]);
        }


        private void SetDayButtonClickedStyle(Button button)
        {
            // Reset the style of the previously selected button
            if (_lastSelectedDayButton != null)
            {
                //apply default button style of winui
                _lastSelectedDayButton.Style = (Style) Application.Current.Resources["DefaultButtonStyle"];
            }

            // Apply the style to the selected button
            button.Style = (Style) Resources["SelectedDayButtonStyle"];

            // Keep track of the selected button
            _lastSelectedDayButton = button;
        }

        private void EmulateDayButtonClick(int index)
        {
            var button = (Button) repeaterDays.GetOrCreateElement(index);

            //emulate click on button
            var ap = new ButtonAutomationPeer(button);
            var ip = ap.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            ip?.Invoke();
        }


        private void ReloadButton_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(LoadApiData);
        }

        private void NextPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            //navigate to next page
            Frame.Navigate(typeof(BlankPage1));
        }

        private async void UnitsSegmented_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var segmented = (Segmented) sender;

            //save selected index to local settings
            await ApplicationData.Current.LocalSettings.SaveAsync("selectedUnits", segmented.SelectedIndex);

            await Task.Run(LoadApiData);
        }
    }
}
