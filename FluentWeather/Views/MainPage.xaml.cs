﻿using System;
using FluentWeather.ViewModels;
using System.Net.Http;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentWeather.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using FluentWeather.Adapters;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Input;
using FluentWeather.Helpers;
using FluentWeather.Services;
using FluentWeather.Utils;
using FluentWeather.Dialogs;
using FluentWeather.Core.Helpers;
using CommunityToolkit.WinUI.Controls;
using Windows.System;
using FluentWeather.Controls;

namespace FluentWeather.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel MainPageViewModel { get; } = new();

        public NavigationViewViewModel NavigationViewViewModel { get; } = new();

        public AcrylicEffectsService AcrylicEffectsService { get; } = Singleton<AcrylicEffectsService>.Instance;

        private LiveTileService LiveTileService { get; } = Singleton<LiveTileService>.Instance;

        private readonly AppViewModel _appViewModel = AppViewModelHolder.GetViewModel();

        private static readonly HttpClient SharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/"),
        };

        private static readonly string SystemLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

        private RootV3Response _lastApiData;

        private Button _lastSelectedDayButton;

        private readonly DispatcherTimer _refreshTimer = new();

        public static int SettingsUnitsSelectedIndex
        {
            get
            {
                return Task.Run(async () => await ApplicationData.Current.LocalSettings.ReadAsync<int>("selectedUnits")).Result;
            }
        }

        public static int SettingsTimeFormatSelectedIndex
        {
            get
            {
                //run in background to avoid a deadlock/ui freeze
                return Task.Run(async () => await ApplicationData.Current.LocalSettings.ReadAsync<bool>("is12HourFormat")).Result ? 1 : 0;
            }
        }

        public static bool BackgroundImageToggleStatus
        {
            get
            {
                //run in background to avoid a deadlock/ui freeze
                return Task.Run(async () => await ApplicationData.Current.LocalSettings.ReadAsync<bool>("backgroundImageEnabled", true)).Result;
            }
        }

        public static int AutoRefreshPeriodSelectedValue
        {
            get
            {
                //run in background to avoid a deadlock/ui freeze
                return Task.Run(async () => await ApplicationData.Current.LocalSettings.ReadAsync<int>("autoRefreshDataMinutes")).Result;
            }
        }

        private async void VersionText_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WhatsNewDialog();
            await dialog.ShowAsync();
        }

        public MainPage()
        {
            //use the EntranceNavigationTransition when showing the page at startup (pageload)
            Transitions = [new EntranceThemeTransition()];

            InitializeComponent();

            DataContext = this; //DataContext = ViewModel;
            Initialize();
        }

        private void Initialize()
        {
            // Hide default title bar.
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            AppTitleTextBlock.Text = Package.Current.DisplayName;
            Window.Current.SetTitleBar(TitleBarGrid);

            //remove the solid-colored backgrounds behind the caption controls and system back button
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            //NavigationViewViewModel.Initialize(contentFrame, navigationView, KeyboardAccelerators);

            //pagina di default
            //NavigationService.Navigate(typeof(Views.DashWet));

            MainPageViewModel.AppVersionText = GetVersionDescription();

            InitializeStoryboards();

            _appViewModel.UpdateUiAction += async () => { await Task.Run(LoadApiData); };
            KeyDown += MainPage_KeyDown;

            AutoSuggestBoxMain.TextChanged += AutoSuggestBox_TextChanged;

            _appViewModel.UpdateUi();
            // Initialize the refresh timer
            _refreshTimer.Tick += RefreshTimerTick;
        }

        private async void RefreshTimerTick(object sender, object e)
        {
            await LoadApiData();
        }

        private static string GetVersionDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        internal static async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            sender.ShowHideCustomHeader(true);
            sender.ShowHideCustomHeader(false, AutoSuggestBoxClassExtensions.AutoSuggestBoxHeaderType.NetworkError); //TODO: remove this line and clear all messages automatically in BetterAutosuggestBox.cs

            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput
                && !string.IsNullOrEmpty(sender.Text))
            {
                try
                {
                    var response = await SharedClient.GetAsync("v3/location/searchflat?query=" + sender.Text +
                                                               "&language=" +
                                                               SystemLanguage +
                                                               "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230&format=json");
                    //&locationType=city (x solo citta)

                    //response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var myDeserializedClass = JsonConvert.DeserializeObject<SearchLocationResponse>(jsonResponse);
                        var finalitems = myDeserializedClass.location.Select(x => x).ToList();
                        sender.ItemsSource = finalitems;
                    }
                }
                catch (HttpRequestException e)
                {
                    sender.ShowHideCustomHeader(true, AutoSuggestBoxClassExtensions.AutoSuggestBoxHeaderType.NetworkError);
                }
            }

            sender.ShowHideCustomHeader(false);
        }

        private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var selectedPlaceId = ((SearchedLocation) args.SelectedItem).placeId;

            //save location to settings
            await ApplicationData.Current.LocalSettings.SaveAsync("lastPlaceId", selectedPlaceId);
            _appViewModel.UpdateUi();
        }

        private async Task LoadApiData()
        {
            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    RefreshButton.Visibility = Visibility.Collapsed;
                    MainPageViewModel.IsLoadingData = true;
                }
            );

            string lastPlaceId = await ApplicationData.Current.LocalSettings.ReadAsync<string>("lastPlaceId");
            try
            {
                var response = await ApiUtils.GetFullData(lastPlaceId);

                if (response != null)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _lastApiData = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);

                    LiveTileService.UpdateWeatherMainTile(_lastApiData);

                    await CoreApplication.MainView.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal, () =>
                        {
                            UpdateUi(_lastApiData);
                            MainInnerContentGrid.Visibility = Visibility.Visible;
                        }
                    );
                }
            }
            catch (HttpRequestException e)
            {
                CoreApplication.MainView.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    async void () =>
                    {
                        ContentDialog noWifiDialog = new()
                        {
                            Title = "No internet connection",
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Content = "To continue, you need to connect to the internet.",
                            CloseButtonText = "OK"
                        };
                        try
                        {
                            await noWifiDialog.ShowAsync();
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                    }
                );

            }

            await CoreApplication.MainView.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    RefreshButton.Visibility = Visibility.Visible;
                    MainPageViewModel.IsLoadingData = false;
                }
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
            BitmapImage newImage = newImageUri == null ? new() : new(newImageUri);

            if (_isImage1Active)
                Image2.Source = newImage;
            else
                Image1.Source = newImage;

            if (newImageUri == null)
                ResetStartStoryboard();
            else
            {
                //remeber that the imageopened event is fired only when the image is shown in some way :( so we need to set the image source before

                //this ensures the animation is played only when the image is loaded
                newImage.ImageOpened += (sender, args) => { ResetStartStoryboard(); };
            }

            return;

            void ResetStartStoryboard()
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
            }
        }

        private void InitializeStoryboards()
        {
            _storyboard1 = CreateCrossfadeStoryboard(Image1, Image2);
            _storyboard2 = CreateCrossfadeStoryboard(Image2, Image1);
            _isImage1Active = true;
        }

        private Storyboard CreateCrossfadeStoryboard(Image fadeInImage, Image fadeOutImage)
        {
            DoubleAnimation fadeInAnimation = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fadeInAnimation, fadeInImage);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");

            DoubleAnimation fadeOutAnimation = new()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(fadeOutAnimation, fadeOutImage);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");

            Storyboard storyboard = new();
            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);

            return storyboard;
        }

        private async void UpdateUi(RootV3Response rootV3Response)
        {
            CrossfadeToImageApiData(rootV3Response);

            var newIconUri = new Uri("ms-appx:///Assets/weticons/" + rootV3Response.v3wxobservationscurrent.iconCode +
                                     ".svg");

            //get svgimagesource object from image source
            var svgImageSource = (SvgImageSource) MainIcon.Source;
            svgImageSource.UriSource = newIconUri;
            MainIcon.Source = svgImageSource;

            //update chips
            WetUnits currentUnits = await VariousUtils.GetUnitsCode();

            UpdatedOnText.Text = "UpdatedOnText".GetLocalized() + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " " +
                                 TimeZoneInfo.Local.Id;

            //join the variables displayName, city, adminDistrict, country together in a single string with a comma between each variable (some may be empty) and remove duplicate names
            string placeName = string.Join(", ",
                new[]
                {
                    rootV3Response.v3locationpoint.LocationV3.displayName,
                    rootV3Response.v3locationpoint.LocationV3.city,
                    rootV3Response.v3locationpoint.LocationV3.adminDistrict,
                    rootV3Response.v3locationpoint.LocationV3.country
                }.Where(s => !string.IsNullOrEmpty(s)).Distinct());

            PlaceText.Text = placeName;

            MainPhraseText.Text = rootV3Response.v3wxobservationscurrent.cloudCoverPhrase;

            CurrentTempText.Text = rootV3Response.v3wxobservationscurrent.temperature + "°" +
                                   MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            FeelsLikeText.Text = "FeelsLikeText".GetLocalized() + " " +
                                 rootV3Response.v3wxobservationscurrent.temperatureFeelsLike + "°" +
                                 MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            WindChipControl.Value = rootV3Response.v3wxobservationscurrent.windSpeed + " " +
                                    MeasureUnitUtils.GetWindSpeedUnits(currentUnits) + " " +
                                    rootV3Response.v3wxobservationscurrent.windDirectionCardinal;

            //CloudCoverChipControl.Value = rootV3Response.v3wxobservationscurrent.phrase;

            HumidityChipControl.Value = rootV3Response.v3wxobservationscurrent.relativeHumidity + "%";

            PressureChipControl.Value = rootV3Response.v3wxobservationscurrent.pressureMeanSeaLevel + " " +
                                        MeasureUnitUtils.GetPressureUnits(currentUnits);

            VisibilityChipControl.Value = rootV3Response.v3wxobservationscurrent.visibility + " " +
                                          MeasureUnitUtils.GetVisibilityUnits(currentUnits);

            DewPointChipControl.Value = rootV3Response.v3wxobservationscurrent.temperatureDewPoint + "°" +
                                        MeasureUnitUtils.GetTemperatureUnits(currentUnits);

            UVIndexChipControl.Value = rootV3Response.v3wxobservationscurrent.uvIndex + " (" +
                                       rootV3Response.v3wxobservationscurrent.uvDescription + ")";

            //update days repeater itemssource with creating for every day a new DayButtonAdapter class

            List<DayButtonAdapter> dayButtonAdapters = [];

            int i = 0;

            foreach (var day in rootV3Response.v3wxforecastdaily10day.dayOfWeek)
            {
                dayButtonAdapters.Add(new DayButtonAdapter(rootV3Response.v3wxforecastdaily10day, i));
                i++;
            }

            RepeaterDays.ItemsSource = dayButtonAdapters;

            //select first day button
            EmulateDayButtonClick(0);
        }

        private void CrossfadeToImageApiData(RootV3Response rootV3Response = null)
        {
            if (!BackgroundImageToggleStatus)
                return;

            rootV3Response ??= _lastApiData;

            var newImageUri = new Uri("ms-appx:///Assets/bgs/" +
                                      IconsDictionary.IconCodeToBackgroundImageNameDictionary[
                                          rootV3Response.v3wxobservationscurrent.iconCode.ToString()] + ".jpg");

            CrossfadeToImage(newImageUri);
        }

        private async void LoadHourlyData(DateTimeOffset dayToLoad)
        {
            List<HourDataAdapter> hourlyDataAdapters = [];

            WetUnits currentUnits = await VariousUtils.GetUnitsCode();

            int i = 0;

            var firstDate = _lastApiData.v3wxforecasthourly10day.validTimeLocal[0];

            //subtract daytoload to firstdate to get the index of the first day to load
            int daysDiff = dayToLoad.Date.Subtract(firstDate.Date).Days;

            if (daysDiff == -1 ||
                (daysDiff == 0 &&
                 _lastApiData.v3wxforecasthourly10day.validTimeLocal[0].Hour >
                 7)) //this means that we are still in a tonight/today state and we add hours only until 7AM
            {
                int h = 0;

                while (_lastApiData.v3wxforecasthourly10day.validTimeLocal[h].Hour != 7)
                {
                    hourlyDataAdapters.Add(new HourDataAdapter(_lastApiData.v3wxforecasthourly10day, h, currentUnits));

                    h++;
                }
            }
            else
            {
                foreach (var date in _lastApiData.v3wxforecasthourly10day.validTimeLocal)
                {
                    //the .Date property is used to compare only the date part of the datetime without the time (no hours, minutes, seconds)
                    if (date.Date == firstDate.Date.AddDays(daysDiff))
                    {
                        hourlyDataAdapters.Add(new HourDataAdapter(_lastApiData.v3wxforecasthourly10day, i,
                            currentUnits));
                    }

                    i++;
                }
            }

            HourlyListview.ItemsSource = hourlyDataAdapters;
        }

        private async void LoadInsightsData(DateTimeOffset dayToLoad)
        {
            /*var indexOfDay =
                lastApiData.v2idxDriveDaypart10days.drivingDifficultyIndex12hour.fcstValidLocal.IndexOf(dayToLoad.Date);*/

            var indexOfDayInsights =
                _lastApiData.v2idxDriveDaypart10days.drivingDifficultyIndex12hour.fcstValidLocal.FindIndex(x =>
                    x.Date == dayToLoad.Date);

            RunningInsight.Insight = new Insight
            {
                Title = "RunningInsightTitle".GetLocalized(),
                Value =
                    _lastApiData.v2idxRunDaypart10days.RunWeatherIndexDaypart.longRunWeatherIndex[indexOfDayInsights],
                Description =
                    _lastApiData.v2idxRunDaypart10days.RunWeatherIndexDaypart.longRunWeatherCategory
                        [indexOfDayInsights],
                Levels = InsightLevels.RunningLevels,
                IconName = "running"
            };

            DrivingInsight.Insight = new Insight
            {
                Title = "DrivingInsightTitle".GetLocalized(),
                Value =
                    _lastApiData.v2idxDriveDaypart10days.drivingDifficultyIndex12hour.drivingDifficultyIndex[
                        indexOfDayInsights],
                Description =
                    _lastApiData.v2idxDriveDaypart10days.drivingDifficultyIndex12hour.drivingDifficultyCategory[
                        indexOfDayInsights],
                Levels = InsightLevels.DrivingLevels,
                IconName = "driving"
            };

            /*PollenInsight.Insight = new Insight() {Title = "Pollen",
                Value = rootV3Response.v2idxPollenDaypart10days.PollenForecastDaypart.ind[0],
                Description = rootV3Response.v2idxRunDaypart10days.RunWeatherIndexDaypart.longRunWeatherCategory[0],
                Levels = InsightLevels.RunningLevels};*/

            DryskinInsight.Insight = new Insight
            {
                Title = "DrySkinInsightTitle".GetLocalized(),
                Value = _lastApiData.V2IdxDrySkinDaypart10days.DrySkinIndexDaypart.drySkinIndex[indexOfDayInsights],
                Description =
                    _lastApiData.V2IdxDrySkinDaypart10days.DrySkinIndexDaypart.drySkinCategory[indexOfDayInsights],
                Levels = InsightLevels.DrySkinLevels,
                IconName = "dry"
            };

            WateringInsight.Insight = new Insight
            {
                Title = "WateringNeedInsightTitle".GetLocalized(),
                Value =
                    _lastApiData.V2IdxWateringDaypart10days.WateringNeedsIndexDaypart.wateringNeedsIndex[
                        indexOfDayInsights],
                Description =
                    _lastApiData.V2IdxWateringDaypart10days.WateringNeedsIndexDaypart.wateringNeedsCategory[
                        indexOfDayInsights],
                Levels = InsightLevels.WateringLevels,
                IconName = "watering"
            };

            var indexOfDayDailyData =
                _lastApiData.v3wxforecastdaily10day.validTimeLocal.FindIndex(x =>
                    x.Date == dayToLoad.Date);

            //day summary
            var daySummaryString = _lastApiData.v3wxforecastdaily10day.daypart[0].narrative[indexOfDayDailyData * 2];

            DaySummaryText.Text = daySummaryString ?? "--";
            NightSummaryText.Text =
                _lastApiData.v3wxforecastdaily10day.daypart[0].narrative[indexOfDayDailyData * 2 + 1];

            //sunset and sunrise
            var sunriseTime = _lastApiData.v3wxforecastdaily10day.sunriseTimeLocal[indexOfDayDailyData];
            SunriseTimeText.Text = sunriseTime == null ? "--" : await VariousUtils.GetTimeBasedOnUserSettings(sunriseTime.Value.DateTime);

            var sunsetTime = _lastApiData.v3wxforecastdaily10day.sunsetTimeLocal[indexOfDayDailyData];
            SunsetTimeText.Text = sunsetTime == null ? "--" : await VariousUtils.GetTimeBasedOnUserSettings(sunsetTime.Value.DateTime);

            //moonrise and moonset
            var moonriseTime = _lastApiData.v3wxforecastdaily10day.moonriseTimeLocal[indexOfDayDailyData];
            MoonriseTimeText.Text = moonriseTime == null ? "--" : await VariousUtils.GetTimeBasedOnUserSettings(moonriseTime.Value.DateTime);

            var moonsetTime = _lastApiData.v3wxforecastdaily10day.moonsetTimeLocal[indexOfDayDailyData];
            MoonsetTimeText.Text = moonsetTime == null ? "--" : await VariousUtils.GetTimeBasedOnUserSettings(moonsetTime.Value.DateTime);

            //lunar phase
            var lunarPhase = _lastApiData.v3wxforecastdaily10day.moonPhase[indexOfDayDailyData];
            LunarphaseText.Text = lunarPhase;

            //moon phase icon
            var moonPhaseIcon = _lastApiData.v3wxforecastdaily10day.moonPhaseCode[indexOfDayDailyData];

            var svgImageSource = new SvgImageSource(new Uri($"ms-appx:///Assets/varicons/{moonPhaseIcon}.svg"));
            LunarphaseIcon.Source = svgImageSource;
        }

        private void DayButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;

            SetDayButtonClickedStyle(button);

            //get button's DayButtonAdapter object
            var dayButtonAdapter = (DayButtonAdapter) button.DataContext;

            //load hourly data
            LoadHourlyData(dayButtonAdapter.CurrentObject.validTimeLocal[dayButtonAdapter.ItemIndex]);
            LoadInsightsData(dayButtonAdapter.CurrentObject.validTimeLocal[dayButtonAdapter.ItemIndex]);
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
            var button = (Button) RepeaterDays.GetOrCreateElement(index);

            //emulate click on button
            var ap = new ButtonAutomationPeer(button);
            var ip = ap.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            ip?.Invoke();
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            _appViewModel.UpdateUi();
        }

        private async void UnitsSegmented_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var segmented = (Segmented) sender;

            if (!segmented.IsLoaded)
                return;

            //save selected index to local settings
            await ApplicationData.Current.LocalSettings.SaveAsync("selectedUnits", segmented.SelectedIndex);
            _appViewModel.UpdateUi();
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // Only keeps the suggestions list open if no suggestion was chosen.
            sender.IsSuggestionListOpen = true;
        }

        private void AutoSuggestBoxMain_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            AutoSuggestBox sender2 = (AutoSuggestBox) sender;
            sender2.IsSuggestionListOpen = true;
        }

        private async void AcrylicToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            var acrylicToggle = (ToggleSwitch) sender;

            //save selected index to local settings
            await ApplicationData.Current.LocalSettings.SaveAsync("effectsEnabled", acrylicToggle.IsOn);
            await AcrylicEffectsService.SetThemeAsync(acrylicToggle.IsOn);
        }

        private void RefContainer_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            _appViewModel.UpdateUi();
        }

        private async void TimeFormatSegmented_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var segmented = (Segmented) sender;

            if (!segmented.IsLoaded)
                return;

            //save selected index to local settings
            await ApplicationData.Current.LocalSettings.SaveAsync("is12HourFormat", segmented.SelectedIndex == 1);
            _appViewModel.UpdateUi();
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs args)
        {
            switch (args.Key)
            {
                case VirtualKey.F5:
                    _appViewModel.UpdateUi();
                    break;
            }
        }

        private async void BackgroundImageToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            var backgroundImageToggle = (ToggleSwitch)sender;
            await ApplicationData.Current.LocalSettings.SaveAsync("backgroundImageEnabled", backgroundImageToggle.IsOn);

            if (backgroundImageToggle.IsOn)
                CrossfadeToImageApiData();
            else
                CrossfadeToImage(null);
        }

        private async void AutoRefreshComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var autoRefreshComboBox = (ComboBox)sender;
            var selectedItem = (ComboBoxItem)autoRefreshComboBox.SelectedItem;
            if (selectedItem?.Tag != null)
            {
                var minutes = Convert.ToDouble(selectedItem.Tag);
                if (minutes > 0)
                {
                    _refreshTimer.Interval = TimeSpan.FromSeconds(minutes);
                    _refreshTimer.Start();
                }
                else
                    _refreshTimer.Stop();

                await ApplicationData.Current.LocalSettings.SaveAsync("autoRefreshDataMinutes", minutes);
            }
        }
    }
}
