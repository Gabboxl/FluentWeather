using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace FluentWeather.Converters
{
    public class BetterBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            /*
             * If param is true means that the control will be visible if the value is true
             * other way around, if param is false, the control will be visible if the value is false
             *
             */

            bool param = true;

            if (parameter != null)
                param = System.Convert.ToBoolean(parameter);

            bool state = (bool) value;

            Visibility visibility = Visibility.Visible;

            if (state != param)
                visibility = Visibility.Collapsed;

            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new NotImplementedException();
        }
    }
}
