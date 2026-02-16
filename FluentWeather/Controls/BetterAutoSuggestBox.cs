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

        /*
        // COM shit stolen from https://github.com/hexawyz/Exo/commit/e23ac45a9684f4d3f22d9a8947c0cda68f0bf93c
        public static bool TryAs<TInterface>(object? value, [NotNullWhen(true)] out TInterface? result)
		where TInterface : class
	    {
		    if (value is null) goto NotSuccessful;

		    if (value is TInterface i)
		    {
			    result = i;
			    return true;
		    }

		    ObjectReference<IUnknownVftbl> unknown;

		    if (ComWrappersSupport.TryUnwrapObject(value, out var objRef))
		    {
			    unknown = objRef.As<IUnknownVftbl>(IID.IID_IUnknown);
		    }
		    else if (value is IWinRTObject winRTObject)
		    {
			    unknown = winRTObject.NativeObject.As<IUnknownVftbl>(IID.IID_IUnknown);
		    }
		    else
		    {
			    goto NotSuccessful;
		    }

		    using (unknown)
		    {
			    if (typeof(TInterface).IsDefined(typeof(ComImportAttribute)))
			    {
				    Guid iid = typeof(TInterface).GUID;
				    var qir = Marshal.QueryInterface(unknown.ThisPtr, in iid, out var ppv);
				    if (qir >= 0)
				    {
					    try
					    {
						    result = (TInterface)Marshal.GetObjectForIUnknown(ppv);
					    }
					    finally
					    {
						    Marshal.Release(ppv);
					    }
				    }
				    else if ((uint)qir == 0x80004002U)
				    {
					    goto NotSuccessful;
				    }
				    else
				    {
					    Marshal.ThrowExceptionForHR(qir);
				    }
			    }
		    }
	    NotSuccessful:;
		    result = null;
		    return false;
	    }

	    private static TElement? FindChild<TElement>(FrameworkElement element, string name)
		    where TElement : FrameworkElement
	    {
		    int childCount = VisualTreeHelper.GetChildrenCount(element);
		    if (childCount > 0)
		    {
			    return FindChild<TElement>(element, childCount, name);
		    }
		    return null;
	    }

	    private static TElement? FindChild<TElement>(FrameworkElement element, int childCount, string name)
		    where TElement : FrameworkElement
	    {
		    for (int i = 0; i < childCount; i++)
		    {
			    if (VisualTreeHelper.GetChild(element, i) is not FrameworkElement child) continue;
			    if (child.Name == name && TryAs<TElement>(child, out var typedChild)) return typedChild;
			    int count = VisualTreeHelper.GetChildrenCount(child);
			    if (count > 0 && FindChild<TElement>(child, count, name) is { } childChild) return childChild;
		    }
		    return null;
	    }

        // example usage
        //if(!TryAs<Border>(popups[0].Child, out var border)) return;
        //if(!TryAs<ListView>(border.Child, out var listView)) return;
        //if(!TryAs<StackPanel>((IWinRTObject)listView.Header, out var stack)) return;

        */

        public static void ShowHideCustomHeader(this AutoSuggestBox mySealedClass, bool show, AutoSuggestBoxHeaderType type = AutoSuggestBoxHeaderType.Loading)
        {
            var popups = new List<Popup>();

            VisualTreeSearchHelper.FindChildren(popups, mySealedClass);

            if (!popups.Any())
            {
                return;
            }

            // More info on WinRT As() method and why it is needed for AOT instead of a language cast here:
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
