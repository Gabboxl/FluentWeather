using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentWeather.Helpers;

namespace FluentWeather.Dialogs
{
    public sealed partial class WhatsNewDialog : ContentDialog
    {
        public WhatsNewDialog()
        {
            // TODO: Update the contents of this dialog every time you release a new version of the app
            RequestedTheme = (Window.Current.Content as FrameworkElement).RequestedTheme;
            InitializeComponent();

            WhatsNewTextBlock.Text = "WhatsNew_Body".GetLocalized(true);
        }
    }
}
