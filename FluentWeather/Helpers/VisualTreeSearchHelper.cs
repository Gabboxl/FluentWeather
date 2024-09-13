using System.Collections.Generic;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace FluentWeather.Helpers
{
    public static class VisualTreeSearchHelper
    {
        public static void FindChildren<T>(List<T> results, DependencyObject startNode) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);

            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);

                if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
                {
                    T asType = (T)current;
                    results.Add(asType);
                }

                FindChildren<T>(results, current);
            }
        }

        //public static T FindChild<T>(DependencyObject startNode, string name) where T : DependencyObject
        //{
        //    int count = VisualTreeHelper.GetChildrenCount(startNode);

        //    for (int i = 0; i < count; i++)
        //    {
        //        DependencyObject current = VisualTreeHelper.GetChild(startNode, i);

        //        if (current is T && (current as FrameworkElement)?.Name == name)
        //        {
        //            return (T)current;
        //        }

        //        T result = FindChild<T>(current, name);
        //        if (result != null)
        //        {
        //            return result;
        //        }
        //    }

        //    return null;
        //}
    }
}
