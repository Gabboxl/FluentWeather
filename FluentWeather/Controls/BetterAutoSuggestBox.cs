using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using FluentWeather.Helpers;

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
        public enum AutoSuggestBoxHeaderType
        {
            Loading,
            NetworkError
        }

        // Extension method to double the Value property
        public static void ShowHideCustomHeader(this AutoSuggestBox mySealedClass, bool show, AutoSuggestBoxHeaderType type = AutoSuggestBoxHeaderType.Loading)
        {
            var popups = new List<Popup>();

            VisualTreeSearchHelper.FindChildren(popups, mySealedClass);

            if (!popups.Any())
            {
                return;
            }

            Grid element = null;
            var stack = ((popups[0].Child as Border).Child as ListView).Header as StackPanel;

            switch (type)
            {
                case AutoSuggestBoxHeaderType.Loading:
                    element = stack.Children[0] as Grid;
                    break;

                case AutoSuggestBoxHeaderType.NetworkError:
                    element = stack.Children[1] as Grid;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            //    element.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

            //check if any of stack children is visible
            //if so, hide it
            
            //(grid.Children[1] as TextBlock).Text = "My custom text";
            element.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            int visibleCount = 0;
            foreach (var child in stack.Children)
            {
                if (child is Grid grid)
                {
                    if (grid.Visibility == Visibility.Visible)
                    {
                       // grid.Visibility = Visibility.Collapsed;
                        visibleCount++;
                    }
                }
            }
            stack.Height = visibleCount > 0 ? 50 : 0;
        }
    }
}
