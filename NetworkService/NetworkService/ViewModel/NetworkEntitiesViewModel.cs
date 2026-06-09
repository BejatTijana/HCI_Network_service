using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using NetworkService.Commands;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class NetworkEntitiesViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<NetworkEntity> _source;
        private readonly Action _navigateToAdd;
        private readonly Func<string, bool> _confirmDelete;
        private readonly Action<string, string> _showToast;
        private readonly Action _restartSimulator;
        private readonly Action<Action> _pushUndo;
        public Action<int> ClearConnections { get; set; } = _ => { };
        private string _searchText = "";
        private bool _searchByName = true;
        private bool _searchByType;
        private NetworkEntity _selectedEntity;

        public ObservableCollection<NetworkEntity> FilteredEntities { get; }
            = new ObservableCollection<NetworkEntity>();

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); ApplyFilter(); }
        }

        public bool SearchByName
        {
            get => _searchByName;
            set
            {
                if (_searchByName == value) return;
                _searchByName = value;
                if (value) _searchByType = false;
                OnPropertyChanged(nameof(SearchByName));
                OnPropertyChanged(nameof(SearchByType));
                ApplyFilter();
            }
        }

        public bool SearchByType
        {
            get => _searchByType;
            set
            {
                if (_searchByType == value) return;
                _searchByType = value;
                if (value) _searchByName = false;
                OnPropertyChanged(nameof(SearchByType));
                OnPropertyChanged(nameof(SearchByName));
                ApplyFilter();
            }
        }

        public NetworkEntity SelectedEntity
        {
            get => _selectedEntity;
            set { _selectedEntity = value; OnPropertyChanged(nameof(SelectedEntity)); }
        }

        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand NavigateToAddCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public NetworkEntitiesViewModel(ObservableCollection<NetworkEntity> source, Action navigateToAdd,
            Func<string, bool> confirmDelete, Action<string, string> showToast,
            Action restartSimulator, Action<Action> pushUndo)
        {
            _source = source;
            _navigateToAdd = navigateToAdd;
            _confirmDelete = confirmDelete;
            _showToast = showToast;
            _restartSimulator = restartSimulator;
            _pushUndo = pushUndo;
            _source.CollectionChanged += OnSourceChanged;

            DeleteCommand = new RelayCommand(_ =>
            {
                if (SelectedEntity == null) return;
                if (!_confirmDelete(SelectedEntity.Naziv)) return;
                var capturedEntity = SelectedEntity;
                int capturedIndex = _source.IndexOf(capturedEntity);
                _source.Remove(capturedEntity);
                ClearConnections(capturedEntity.ID);
                SelectedEntity = null;
                _showToast("Entity Deleted", $"\"{capturedEntity.Naziv}\" removed.");
                _restartSimulator();
                _pushUndo(() => _source.Insert(
                    Math.Min(capturedIndex, _source.Count), capturedEntity));
            }, _ => SelectedEntity != null);

            ClearSearchCommand = new RelayCommand(_ => SearchText = "");
            NavigateToAddCommand = new RelayCommand(_ => _navigateToAdd());

            ApplyFilter();
        }

        private void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
            => ApplyFilter();

        private void ApplyFilter()
        {
            FilteredEntities.Clear();
            var term = _searchText?.ToLowerInvariant() ?? "";
            foreach (var e in _source)
            {
                bool match = string.IsNullOrEmpty(term)
                    || (_searchByName && e.Naziv != null && e.Naziv.ToLowerInvariant().Contains(term))
                    || (_searchByType && e.Tip != null && e.Tip.Ime != null && e.Tip.Ime.ToLowerInvariant().Contains(term));
                if (match) FilteredEntities.Add(e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
