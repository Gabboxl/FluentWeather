using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FluidWeather.Models;
using Windows.UI.Xaml.Media.Imaging;

namespace FluidWeather.Controls
{
    public sealed partial class InsightControl : UserControl
    {
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
                new PropertyMetadata(null, new PropertyChangedCallback(OnTitleChanged)));

        private static async void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var currentInstance = (InsightControl) d;

            var newValue = (Insight) e.NewValue;

            currentInstance.TitleText.Text = newValue.Title;
            currentInstance.ValueText.Text = newValue.Description + " (" + newValue.Value + ")";
            currentInstance.LevelRectangle.Fill = currentInstance.GetLevelColor(newValue.Value, newValue.Levels);


            var newIcon = new SvgImageSource
            {
                UriSource = new Uri("ms-appx:///Assets/insighticons/" + newValue.IconName + ".svg")
            };

            var test1 = ChangeSvgColor("Assets/insighticons/" + newValue.IconName + ".svg", "#FFFFFF");

            var test2 = await CreateSvgImageSource(test1);

            currentInstance.InsightIcon.Source = test2;
        }

        public static string ChangeSvgColor(string svgFilePath, string newColor)
        {
            string svgContent;

            // Open the file with UTF8 encoding and use the StreamReader constructor that automatically skips the BOM
            using (var reader = new StreamReader(svgFilePath, new UTF8Encoding(true)))
            {
                svgContent = reader.ReadToEnd();
            }

            XDocument svgXml = XDocument.Parse(svgContent);
            XNamespace ns = "http://www.w3.org/2000/svg";

            foreach (XElement path in svgXml.Descendants(ns + "path"))
            {
                XAttribute fillAttribute = path.Attribute("fill");
                if (fillAttribute != null)
                {
                    fillAttribute.Value = newColor;
                }
            }

            return svgXml.ToString();
        }

        private static int lol = 0;

        private static async Task<SvgImageSource> CreateSvgImageSource(string svgXml)
        {
            // Create a temporary file. Delete it if it already exists.
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

            lol++;

            StorageFile tempFile = await tempFolder.CreateFileAsync("temp" + lol +  ".svg", CreationCollisionOption.ReplaceExisting);

            // Write the SVG XML code to the temporary file.
            await FileIO.WriteTextAsync(tempFile, svgXml);


            // Create a new SvgImageSource from the temporary file.
            var svgImageSource = new SvgImageSource(new Uri(tempFile.Path));

            return svgImageSource;
        }

        Brush GetLevelColor(int level, Dictionary<int, InsightLevel> levels)
        {
            var finalLevel = InsightLevel.None;

            SolidColorBrush brush = new SolidColorBrush();

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


            //return brush;
        }


        public InsightControl()
        {
            this.InitializeComponent();
        }
    }
}
