using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluentWeather.Models;
using FluentWeather.Core.Helpers;
using FluentWeather.Services;
using Windows.UI.Xaml.Markup;

namespace FluentWeather.Controls
{
    public sealed partial class InsightControl : UserControl
    {
        public AcrylicEffectsService AcrylicEffectsService { get; } = Singleton<AcrylicEffectsService>.Instance;

        public Insight Insight
        {
            get { return (Insight) GetValue(InsightProperty); }
            set { SetValue(InsightProperty, value); }
        }

        private static readonly DependencyProperty InsightProperty =
            DependencyProperty.Register(
                nameof(Insight),
                typeof(Insight),
                typeof(InsightControl),
                new PropertyMetadata(null, OnTitleChanged));

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var currentInstance = (InsightControl) d;

            var newValue = (Insight) e.NewValue;

            currentInstance.TitleText.Text = newValue.Title;
            currentInstance.ValueText.Text = newValue.Description + " (" + newValue.Value + ")";
            currentInstance.LevelRectangle.Fill = currentInstance.GetLevelColor(newValue.Value, newValue.Levels);


            string iconPath =  (string) Application.Current.Resources[newValue.IconName + "Path"];

            currentInstance.InsightIconPath.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), iconPath);

            //currentInstance.InsightIconPath.Fill = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];

            currentInstance.InsightIconPath.MaxWidth = 24;

        }

        Brush GetLevelColor(int level, Dictionary<int, InsightLevel> levels)
        {
            var finalLevel = InsightLevel.None;

            if (levels.ContainsKey(level))
            {
                finalLevel = levels[level];
            }

            switch (finalLevel)
            {
                case InsightLevel.Low:
                    return (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
                case InsightLevel.Medium:
                    return (SolidColorBrush) Application.Current.Resources["SystemFillColorCautionBrush"];
                case InsightLevel.High:
                    return (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
                default:
                    return (SolidColorBrush) Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"];
            }

        }


        public InsightControl()
        {
            InitializeComponent();
        }
    }
}
