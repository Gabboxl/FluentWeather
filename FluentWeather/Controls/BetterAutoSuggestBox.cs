using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using CommunityToolkit.WinUI;
using FluentWeather.Helpers;
using Microsoft.Toolkit.Uwp.UI;

namespace FluentWeather.Controls
{
    //lol it's not possible because AutoSuggestBox freakin is sealed
    //internal class BetterAutoSuggestBox : AutoSuggestBox
    //{

    //    // Register the dependency property
    //    public static readonly DependencyProperty CustomProperty =
    //        DependencyProperty.Register(
    //            nameof(Custom),
    //            typeof(string),
    //            typeof(BetterAutoSuggestBox),
    //            new PropertyMetadata(default(string)));

    //    // CLR property wrapper
    //    public string Custom
    //    {
    //        get => (string)GetValue(CustomProperty);
    //        set => SetValue(CustomProperty, value);
    //    }

    //}

    public static class AutoSuggestBoxClassExtensions
    {
        // Extension method to double the Value property
        public static void ShowHideLoadingHeader(this AutoSuggestBox mySealedClass, bool show)
        {
            var popups = new List<Popup>();

            VisualTreeSearchHelper.FindChildren(popups, mySealedClass);

            if (!popups.Any())
            {
                return;
            }

            var grid = ((popups[0].Child as Border).Child as ListView).Header as Grid;

            //(grid.Children[1] as TextBlock).Text = "My custom text";
            grid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
