using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        private Point _dragStartPoint;

        private NetworkDisplayViewModel _vm => DataContext as NetworkDisplayViewModel;

        public NetworkDisplayView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null)
                _vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(NetworkDisplayViewModel.Connections))
                        DrawLines();
                };
            DrawLines();
        }

        private void TV_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void TV_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            var tv = (TreeView)sender;
            if (tv.SelectedItem is NetworkEntity entity)
                DragDrop.DoDragDrop(tv, entity.ID.ToString(), DragDropEffects.Move);
        }

        private void Slot_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            var idStr = (string)e.Data.GetData(DataFormats.StringFormat);
            if (!int.TryParse(idStr, out int id)) return;
            var entity = _vm?.GetEntityById(id);
            if (entity == null) return;
            var slot = ((FrameworkElement)sender).DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot == null) return;
            _vm.PlaceEntity(slot, entity);
            e.Handled = true;
        }

        private void Slot_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Slot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var slot = ((FrameworkElement)sender).DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot?.Entity == null) return;
            _vm?.ConnectOrDisconnect(slot.Entity.ID);
            e.Handled = true;
        }

        private void DrawLines()
        {
            ConnectionCanvas.Children.Clear();
            var vm = _vm;
            if (vm == null) return;
            foreach (var (idA, idB) in vm.Connections)
            {
                var containerA = FindSlotContainer(idA);
                var containerB = FindSlotContainer(idB);
                if (containerA == null || containerB == null) continue;
                try
                {
                    var cA = GetSlotCenter(containerA);
                    var cB = GetSlotCenter(containerB);
                    ConnectionCanvas.Children.Add(new Line
                    {
                        X1 = cA.X, Y1 = cA.Y, X2 = cB.X, Y2 = cB.Y,
                        Stroke = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    });
                }
                catch { }
            }
        }

        private FrameworkElement FindSlotContainer(int entityId)
        {
            var vm = _vm;
            if (vm == null) return null;
            for (int i = 0; i < vm.CanvasSlots.Count; i++)
            {
                if (vm.CanvasSlots[i].Entity?.ID == entityId)
                    return SlotsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
            }
            return null;
        }

        private Point GetSlotCenter(FrameworkElement container)
        {
            var t = container.TransformToVisual(ConnectionCanvas);
            return t.Transform(new Point(container.ActualWidth / 2, container.ActualHeight / 2));
        }
    }
}
