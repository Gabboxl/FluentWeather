using FluentWeather.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using WinRT;
using WinRT.Interop;

namespace FluentWeather.Controls
{
    public static class AutoSuggestBoxClassExtensions
    {
        public enum AutoSuggestBoxHeaderType
        {
            Loading,
            NetworkError
        }

        public static void ShowHideCustomHeader(this AutoSuggestBox mySealedClass, bool show, AutoSuggestBoxHeaderType type = AutoSuggestBoxHeaderType.Loading)
        {
            var popups = new List<Popup>();

            VisualTreeSearchHelper.FindChildren(popups, mySealedClass);

            if (!popups.Any())
            {
                return;
            }

            // More info on WinRT As() extension method and why it is needed for AOT instead of a language cast here:
            // https://github.com/microsoft/microsoft-ui-xaml/issues/10060
            //
            // Native AOT compilation's trimming removes all types information so we need to fallback querying COM interfaces directly
            // and then wrapping them back into WinRT objects, which is what As() does under the hood.
            var border = popups[0].Child.As<Border>();
            var listView = border.Child.As<ListView>();
            var stack = listView.Header.As<StackPanel>();

            Grid element;

            switch (type)
            {
                case AutoSuggestBoxHeaderType.Loading:
                    element = (Grid)(stack.Children[0]);
                    break;

                case AutoSuggestBoxHeaderType.NetworkError:
                    element = (Grid)(stack.Children[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            element.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            
            int visibleCount = 0;
            foreach (var child in stack.Children)
            {
                if (child is Grid grid)
                {
                    if (grid.Visibility == Visibility.Visible)
                    {
                        visibleCount++;
                    }
                }
            }

            stack.Height = visibleCount > 0 ? 50 : 0;
        }
    }
}
