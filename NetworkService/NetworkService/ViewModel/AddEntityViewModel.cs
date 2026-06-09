using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using NetworkService.Commands;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class AddEntityViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<NetworkEntity> _entities;
        private readonly Action _navigateBack;
        private readonly Action<string, string> _showToast;
        private readonly Action _restartSimulator;
        private readonly Action<Action> _pushUndo;
        private string _naziv = "";
        private string _nazivError = "";
        private EntityType _selectedType;

        public int ID => _entities.Any() ? _entities.Max(e => e.ID) + 1 : 1;

        public string Naziv
        {
            get => _naziv;
            set { _naziv = value; OnPropertyChanged(nameof(Naziv)); NazivError = ""; }
        }

        public string NazivError
        {
            get => _nazivError;
            set
            {
                _nazivError = value;
                OnPropertyChanged(nameof(NazivError));
                OnPropertyChanged(nameof(ShowNazivError));
            }
        }

        public bool ShowNazivError => !string.IsNullOrEmpty(_nazivError);

        public List<EntityType> Types { get; } =
            new List<EntityType> { EntityType.SolarniPanel, EntityType.Vetrogenerator };

        public EntityType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(nameof(SelectedType)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public AddEntityViewModel(ObservableCollection<NetworkEntity> entities, Action navigateBack,
            Action<string, string> showToast, Action restartSimulator, Action<Action> pushUndo)
        {
            _entities = entities;
            _navigateBack = navigateBack;
            _showToast = showToast;
            _restartSimulator = restartSimulator;
            _pushUndo = pushUndo;
            _selectedType = EntityType.SolarniPanel;

            SaveCommand = new RelayCommand(_ =>
            {
                if (string.IsNullOrWhiteSpace(Naziv))
                {
                    NazivError = "Name* is required";
                    return;
                }
                var newEntity = new NetworkEntity
                {
                    ID = this.ID,
                    Naziv = this.Naziv,
                    Tip = SelectedType,
                    LastValue = 0,
                    LastValueValid = false
                };
                _entities.Add(newEntity);
                _showToast("Entity Added", $"\"{newEntity.Naziv}\" added successfully.");
                _restartSimulator();
                _pushUndo(() => _entities.Remove(newEntity));
                Reset();
                _navigateBack();
            });

            CancelCommand = new RelayCommand(_ => { Reset(); _navigateBack(); });
        }

        private void Reset()
        {
            Naziv = "";
            NazivError = "";
            _selectedType = EntityType.SolarniPanel;
            OnPropertyChanged(nameof(SelectedType));
            OnPropertyChanged(nameof(ID));
        }

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
