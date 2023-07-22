using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FluentWeather.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage1 : Page
    {



        public BlankPage1()
        {
            this.InitializeComponent();
            DrawLineAndGradient();
            
        }

        private void DrawLineAndGradient()
        {
            // Define the line's start and end points
            Point startPoint = new Point(50, 50);
            Point endPoint = new Point(250, 250);

            // Define the points for the PathGeometry
            Point bottomRightPoint = new Point(250, 250);
            Point bottomLeftPoint = new Point(50, 250);
    
            // Create the PathGeometry
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure { StartPoint = startPoint, IsClosed = false, IsFilled = true };
            pathGeometry.Figures.Add(pathFigure);

            // Create the gradient fill
            LinearGradientBrush gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(80, 255, 255, 255), Offset = 0 });
            //gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(120, 255, 255, 255), Offset = 0.5 });
            gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = 1 });

            // Create and add the segments
            LineSegment lineSegment = new LineSegment { Point = endPoint };
            LineSegment bottomRightSegment = new LineSegment { Point = bottomRightPoint };
            LineSegment bottomLeftSegment = new LineSegment { Point = bottomLeftPoint };

            pathFigure.Segments.Add(lineSegment);
            pathFigure.Segments.Add(bottomRightSegment);
            pathFigure.Segments.Add(bottomLeftSegment);

            // Assign the PathGeometry and Fill brush to the Path element
            LinePath.Data = pathGeometry;
            LinePath.Fill = gradientBrush;
        }
    }
}
