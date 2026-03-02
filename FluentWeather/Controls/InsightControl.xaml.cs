using FluentWeather.Models;
using FluentWeather.Services;
using FluentWeather.Utils;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using WinRT;

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

        public static readonly DependencyProperty InsightProperty =
            DependencyProperty.Register(
                nameof(Insight),
                typeof(Insight),
                typeof(InsightControl),
                new PropertyMetadata(null, OnInsightChanged));

        private static void OnInsightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InsightControl currentInstance)
            {
                currentInstance.OnInsightChanged(e);
            }
        }

        private void OnInsightChanged(DependencyPropertyChangedEventArgs e)
        {
            var newValue = e.NewValue.As<Insight>();
            if (newValue == null) return;

            TitleText.Text = newValue.Title;
            ValueText.Text = newValue.Description + " (" + newValue.Value + ")";
            LevelRectangle.Fill = GetLevelColor(newValue.Value, newValue.Levels);


            string iconPath =  (string) Application.Current.Resources[newValue.IconName + "Path"];

            InsightIconPath.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), iconPath);

            InsightIconPath.MaxWidth = 24;

        }

        Brush GetLevelColor(int level, Dictionary<int, InsightLevel> levels)
        {
            var finalLevel = InsightLevel.None;

            if (levels != null && levels.ContainsKey(level))
            {
                finalLevel = levels[level];
            }

            switch (finalLevel)
            {
                case InsightLevel.Low:
                    return Application.Current.Resources["SystemFillColorCriticalBrush"].As<SolidColorBrush>();
                case InsightLevel.Medium:
                    return Application.Current.Resources["SystemFillColorCautionBrush"].As<SolidColorBrush>();
                case InsightLevel.High:
                    return Application.Current.Resources["SystemFillColorSuccessBrush"].As<SolidColorBrush>();
                default:
                    return Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"].As<SolidColorBrush>();
            }

        }


        public InsightControl()
        {
            InitializeComponent();
        }
    }
}
