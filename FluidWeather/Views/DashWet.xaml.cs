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
                var finalitems = new List<Location>();

                //get language code from system (like en-us)
                var language = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

                var response = await sharedClient.GetAsync("location/searchflat?locationType=city&query=" + sender.Text + "&language=" + language + "&apiKey=793db2b6128c4bc2bdb2b6128c0bc230&format=json");

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"{jsonResponse}\n");


                var myDeserializedClass = JsonConvert.DeserializeObject<LocationResponse>(jsonResponse);

                foreach (var location in myDeserializedClass.location)
                {
                    Debug.WriteLine(location.address);
                }

                
                finalitems = myDeserializedClass.location.Select(x => x).ToList();
                // the select statement above is the same as the foreach below



                sender.ItemsSource = finalitems;
            }

        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
