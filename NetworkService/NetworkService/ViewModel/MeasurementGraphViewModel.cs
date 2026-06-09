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
            }
        }

        public List<NetworkEntity.MeasurementPoint> GraphPoints =>
            _selectedEntity?.MeasurementHistory?.ToList()
                ?? new List<NetworkEntity.MeasurementPoint>();

        public MeasurementGraphViewModel(ObservableCollection<NetworkEntity> entities)
        {
            _entities = entities;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => OnPropertyChanged(nameof(GraphPoints));
            _timer.Start();
            SelectedEntity = entities.FirstOrDefault();
        }

        private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(GraphPoints));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
