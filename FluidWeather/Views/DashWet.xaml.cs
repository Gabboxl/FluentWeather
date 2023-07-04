using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using FluidWeather.Models;
using Newtonsoft.Json;
using HttpClient = System.Net.Http.HttpClient;

namespace FluidWeather.Views
{
    public sealed partial class DashWet : Page
    {

        public DashWet()
        {
            this.InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);




        }

        private static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://api.weather.com/v3/"),
        };


        public void GoBack(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = (Frame)Window.Current.Content;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

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

                response.EnsureSuccessStatusCode();

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

            var response = await sharedClient.GetAsync("aggcommon/v3-wx-observations-current;v3-wx-forecast-daily-15day;v3-location-point?format=json&placeid="+ ((SearchedLocation)args.SelectedItem).placeId + "&units=m&language=" +
                                                       language + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230");
            //&locationType=city (x solo citta)

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            //Debug.WriteLine($"{jsonResponse}\n");


            var myDeserializedClass = JsonConvert.DeserializeObject<RootV3Response>(jsonResponse);
        }
    }
}
