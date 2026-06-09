using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetworkService.Model
{
    public class NetworkEntity : INotifyPropertyChanged
    {
        private int _id;
        private string _naziv;
        private EntityType _tip;
        private double _lastValue;
        private bool _lastValueValid;

        public int ID
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(ID)); }
        }

        public string Naziv
        {
            get => _naziv;
            set { _naziv = value; OnPropertyChanged(nameof(Naziv)); }
        }

        public EntityType Tip
        {
            get => _tip;
            set { _tip = value; OnPropertyChanged(nameof(Tip)); }
        }

        public double LastValue
        {
            get => _lastValue;
            set { _lastValue = value; OnPropertyChanged(nameof(LastValue)); }
        }

        public bool LastValueValid
        {
            get => _lastValueValid;
            set { _lastValueValid = value; OnPropertyChanged(nameof(LastValueValid)); }
        }

        public class MeasurementPoint
        {
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsValid { get; set; }
        }

        public List<MeasurementPoint> MeasurementHistory { get; }
            = new List<MeasurementPoint>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
