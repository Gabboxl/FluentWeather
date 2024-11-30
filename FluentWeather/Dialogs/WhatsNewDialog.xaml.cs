using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentWeather.Helpers;

namespace FluentWeather.Dialogs
{
    public sealed partial class WhatsNewDialog : ContentDialog
    {
        public WhatsNewDialog()
        {
            RequestedTheme = ((FrameworkElement) Window.Current.Content).RequestedTheme;
            InitializeComponent();

            WhatsNewTextBlock.Text = "WhatsNew_Body".GetLocalized(true);
        }
    }
}
