using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        private Point _dragStartPoint;
        private bool _slotDragStarted = false;
        private Point _slotDragStartPoint;

        private NetworkDisplayViewModel _vm => DataContext as NetworkDisplayViewModel;

        public NetworkDisplayView() { InitializeComponent(); }

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
            if (!int.TryParse((string)e.Data.GetData(DataFormats.StringFormat), out int id)) return;
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
                ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void Slot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _slotDragStarted = false;
            _slotDragStartPoint = e.GetPosition(null);
        }

        private void Slot_SlotMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var slot = ((FrameworkElement)sender).DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot?.Entity == null) return;
            var pos = e.GetPosition(null);
            var diff = _slotDragStartPoint - pos;
            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _slotDragStarted = true;
            DragDrop.DoDragDrop((FrameworkElement)sender, slot.Entity.ID.ToString(), DragDropEffects.Move);
        }

        private void Slot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_slotDragStarted) { _slotDragStarted = false; return; }
            var slot = ((FrameworkElement)sender).DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot?.Entity == null) return;
            _vm?.ConnectOrDisconnect(slot.Entity.ID);
            e.Handled = true;
        }

        private void TV_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            if (!int.TryParse((string)e.Data.GetData(DataFormats.StringFormat), out int id)) return;
            var slot = _vm?.CanvasSlots.FirstOrDefault(s => s.Entity?.ID == id);
            if (slot == null) return;
            _vm.RemoveFromSlot(slot);
            e.Handled = true;
        }

        private void TV_AreaDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat)
                ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
    }
}
