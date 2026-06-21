using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : INotifyPropertyChanged
    {
        public class CanvasSlot : INotifyPropertyChanged
        {
            private NetworkEntity _entity;

            public NetworkEntity Entity
            {
                get => _entity;
                set
                {
                    if (_entity != null) _entity.PropertyChanged -= OnEntityPropertyChanged;
                    _entity = value;
                    if (_entity != null) _entity.PropertyChanged += OnEntityPropertyChanged;
                    OnPropertyChanged(nameof(Entity));
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsOccupied));
                    OnPropertyChanged(nameof(IsOutOfRange));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }

            public bool IsEmpty => _entity == null;
            public bool IsOccupied => _entity != null;
            public bool IsOutOfRange => _entity != null && !_entity.LastValueValid;
            public string DisplayText => _entity == null
                ? "drag here"
                : $"{_entity.Naziv}\n{_entity.LastValue:F1}MW";

            private bool _isSelectedForConnection;
            public bool IsSelectedForConnection
            {
                get => _isSelectedForConnection;
                set { _isSelectedForConnection = value; OnPropertyChanged(nameof(IsSelectedForConnection)); }
            }

            private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                OnPropertyChanged(nameof(IsOutOfRange));
                OnPropertyChanged(nameof(DisplayText));
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class EntityGroup
        {
            public string TypeName { get; set; }
            public List<NetworkEntity> Entities { get; set; }
        }

        public class ConnectionLineData
        {
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
        }

        private readonly ObservableCollection<NetworkEntity> _entities;
        private readonly Action<Action> _pushUndo;
        private readonly List<(int, int)> _connections = new List<(int, int)>();
        private int? _connectingEntityId;

        public ObservableCollection<CanvasSlot> CanvasSlots { get; }

        public List<EntityGroup> EntityGroups =>
            _entities
                .GroupBy(e => e.Tip.Ime)
                .Select(g => new EntityGroup { TypeName = g.Key, Entities = g.ToList() })
                .ToList();

        public IEnumerable<(int, int)> Connections => _connections;

        public ObservableCollection<ConnectionLineData> ConnectionLines { get; }
            = new ObservableCollection<ConnectionLineData>();

        public NetworkDisplayViewModel(ObservableCollection<NetworkEntity> entities, Action<Action> pushUndo)
        {
            _entities = entities;
            _pushUndo = pushUndo;
            CanvasSlots = new ObservableCollection<CanvasSlot>(
                Enumerable.Range(0, 12).Select(_ => new CanvasSlot()));
            _entities.CollectionChanged += OnEntitiesChanged;
        }

        private void OnEntitiesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var slot in CanvasSlots)
            {
                if (slot.Entity != null && !_entities.Contains(slot.Entity))
                    slot.Entity = null;
            }
            OnPropertyChanged(nameof(EntityGroups));
        }

        public void PlaceEntity(CanvasSlot target, NetworkEntity entity)
        {
            var prevSlot = CanvasSlots.FirstOrDefault(s => s.Entity == entity);
            var prevTargetEntity = target.Entity;
            if (prevSlot != null) prevSlot.Entity = null;
            target.Entity = entity;
            OnPropertyChanged(nameof(EntityGroups));
            _pushUndo(() =>
            {
                target.Entity = prevTargetEntity;
                if (prevSlot != null) prevSlot.Entity = entity;
                OnPropertyChanged(nameof(EntityGroups));
            });
            OnPropertyChanged(nameof(Connections));
            UpdateConnectionLines();
        }

        public void RemoveFromSlot(CanvasSlot slot)
        {
            if (slot?.Entity == null) return;
            var entity = slot.Entity;
            slot.Entity = null;
            ClearConnectionsForEntity(entity.ID);
            OnPropertyChanged(nameof(Connections));
            UpdateConnectionLines();
            _pushUndo(() => slot.Entity = entity);
        }

        public NetworkEntity GetEntityById(int id)
            => _entities.FirstOrDefault(e => e.ID == id);

        private void SetConnectingEntityId(int? id)
        {
            _connectingEntityId = id;
            foreach (var slot in CanvasSlots)
                slot.IsSelectedForConnection = slot.Entity != null && slot.Entity.ID == id;
        }

        public void ConnectOrDisconnect(int entityId)
        {
            if (_connectingEntityId == null)
            {
                SetConnectingEntityId(entityId);
                return;
            }
            if (_connectingEntityId == entityId)
            {
                SetConnectingEntityId(null);
                return;
            }
            int a = Math.Min(_connectingEntityId.Value, entityId);
            int b = Math.Max(_connectingEntityId.Value, entityId);
            var pair = (a, b);
            if (_connections.Contains(pair))
            {
                _connections.Remove(pair);
                _pushUndo(() => { _connections.Add(pair); OnPropertyChanged(nameof(Connections)); UpdateConnectionLines(); });
            }
            else
            {
                _connections.Add(pair);
                _pushUndo(() => { _connections.Remove(pair); OnPropertyChanged(nameof(Connections)); UpdateConnectionLines(); });
            }
            SetConnectingEntityId(null);
            OnPropertyChanged(nameof(Connections));
            UpdateConnectionLines();
        }

        public void ClearConnectionsForEntity(int entityId)
        {
            _connections.RemoveAll(p => p.Item1 == entityId || p.Item2 == entityId);
            OnPropertyChanged(nameof(Connections));
            UpdateConnectionLines();
        }

        private void UpdateConnectionLines()
        {
            ConnectionLines.Clear();
            const double cellW = 85.5, cellH = 74.0;
            foreach (var (idA, idB) in _connections)
            {
                int iA = -1, iB = -1;
                for (int i = 0; i < CanvasSlots.Count; i++)
                {
                    if (CanvasSlots[i].Entity?.ID == idA) iA = i;
                    if (CanvasSlots[i].Entity?.ID == idB) iB = i;
                }
                if (iA < 0 || iB < 0) continue;
                ConnectionLines.Add(new ConnectionLineData
                {
                    X1 = (iA % 4) * cellW + cellW / 2,
                    Y1 = (iA / 4) * cellH + cellH / 2,
                    X2 = (iB % 4) * cellW + cellW / 2,
                    Y2 = (iB / 4) * cellH + cellH / 2
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
