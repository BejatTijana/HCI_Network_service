using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    public partial class MeasurementGraphView : UserControl
    {
        private MeasurementGraphViewModel _vm => DataContext as MeasurementGraphViewModel;

        public MeasurementGraphView() { InitializeComponent(); }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
            DrawGraph();
        }

        private void OnVmPropertyChanged(object sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MeasurementGraphViewModel.GraphPoints)
                || e.PropertyName == null)
                DrawGraph();
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();
            var points = _vm?.GraphPoints;
            if (points == null || points.Count == 0) return;

            double minVal = points.Min(p => p.Value);
            double maxVal = points.Max(p => p.Value);
            double range = maxVal - minVal;
            if (range < 0.1) range = 1.0;

            int n = points.Count;
            double[] cx = new double[n];
            double[] cy = new double[n];
            for (int i = 0; i < n; i++)
            {
                cx[i] = 60 + i * 60;
                cy[i] = 20 + (1.0 - (points[i].Value - minVal) / range) * 160;
            }

            for (int i = 0; i < n - 1; i++)
            {
                GraphCanvas.Children.Add(new Line
                {
                    X1 = cx[i], Y1 = cy[i], X2 = cx[i + 1], Y2 = cy[i + 1],
                    Stroke = Brushes.Black, StrokeThickness = 1
                });
            }

            for (int i = 0; i < n; i++)
            {
                var p = points[i];

                var ellipse = new Ellipse { Width = 32, Height = 32 };
                if (p.IsValid)
                {
                    ellipse.Fill = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));
                    ellipse.Stroke = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                    ellipse.StrokeThickness = 1;
                }
                else
                {
                    ellipse.Fill = Brushes.White;
                    ellipse.Stroke = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                    ellipse.StrokeThickness = 1;
                    ellipse.StrokeDashArray = new DoubleCollection { 4, 2 };
                    ellipse.Effect = new DropShadowEffect
                        { BlurRadius = 4, ShadowDepth = 2, Opacity = 0.25, Color = Colors.Black };
                }
                Canvas.SetLeft(ellipse, cx[i] - 16);
                Canvas.SetTop(ellipse, cy[i] - 16);
                GraphCanvas.Children.Add(ellipse);

                var valueLabel = new TextBlock
                {
                    Text = p.Value.ToString("F1"), FontSize = 12,
                    Width = 32, TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(valueLabel, cx[i] - 16);
                Canvas.SetTop(valueLabel, cy[i] - 8);
                GraphCanvas.Children.Add(valueLabel);

                var tsLabel = new TextBlock
                {
                    Text = p.Timestamp.ToString("HH:mm", CultureInfo.InvariantCulture), FontSize = 8,
                    Width = 32, TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))
                };
                Canvas.SetLeft(tsLabel, cx[i] - 16);
                Canvas.SetTop(tsLabel, 185);
                GraphCanvas.Children.Add(tsLabel);
            }
        }
    }
}
