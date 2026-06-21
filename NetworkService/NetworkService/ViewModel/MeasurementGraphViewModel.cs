using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class MeasurementGraphViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<NetworkEntity> _entities;
        private NetworkEntity _selectedEntity;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<NetworkEntity> Entities => _entities;

        public NetworkEntity SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (_selectedEntity != null) _selectedEntity.PropertyChanged -= OnEntityPropertyChanged;
                _selectedEntity = value;
                if (_selectedEntity != null) _selectedEntity.PropertyChanged += OnEntityPropertyChanged;
                OnPropertyChanged(nameof(SelectedEntity));
                OnPropertyChanged(nameof(GraphPoints));
                UpdateDisplayShapes();
            }
        }

        public List<NetworkEntity.MeasurementPoint> GraphPoints =>
            _selectedEntity?.MeasurementHistory?.ToList()
                ?? new List<NetworkEntity.MeasurementPoint>();

        public class DisplayEllipseData
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public string ValueText { get; set; }
            public bool IsValid { get; set; }
            public string TimeLabel { get; set; }
        }

        public class DisplayLineData
        {
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
        }

        public ObservableCollection<DisplayEllipseData> DisplayEllipses { get; }
            = new ObservableCollection<DisplayEllipseData>();
        public ObservableCollection<DisplayLineData> DisplayLines { get; }
            = new ObservableCollection<DisplayLineData>();

        public MeasurementGraphViewModel(ObservableCollection<NetworkEntity> entities)
        {
            _entities = entities;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(GraphPoints));
                UpdateDisplayShapes();
            };
            _timer.Start();
            SelectedEntity = entities.FirstOrDefault();
        }

        private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(GraphPoints));
            UpdateDisplayShapes();
        }

        private void UpdateDisplayShapes()
        {
            DisplayEllipses.Clear();
            DisplayLines.Clear();
            var points = GraphPoints;
            if (points.Count == 0) return;
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
                DisplayLines.Add(new DisplayLineData { X1 = cx[i], Y1 = cy[i], X2 = cx[i + 1], Y2 = cy[i + 1] });
            for (int i = 0; i < n; i++)
                DisplayEllipses.Add(new DisplayEllipseData
                {
                    Left = cx[i] - 16,
                    Top = cy[i] - 16,
                    ValueText = points[i].Value.ToString("F1"),
                    IsValid = points[i].IsValid,
                    TimeLabel = points[i].Timestamp.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
