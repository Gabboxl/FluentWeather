using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentWeather.Controls
{
    public sealed partial class DetailChipControl : UserControl
    {
        public string Title
        {
            get { return (string) GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DetailChipControl),
                new PropertyMetadata(null, new PropertyChangedCallback(OnTitleChanged)));

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var currentInstance = (DetailChipControl) d;

            var newValue = (string) e.NewValue;

            currentInstance.TitleText.Text = newValue;
        }


        public string Value
        {
            get { return (string) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(DetailChipControl),
                new PropertyMetadata(null, new PropertyChangedCallback(OnValueChanged)));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var currentInstance = (DetailChipControl) d;

            var newValue = (string) e.NewValue;

            currentInstance.ValueText.Text = newValue;
        }

        public DetailChipControl()
        {
            InitializeComponent();
        }
    }
}
