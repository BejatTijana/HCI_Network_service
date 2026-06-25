using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkService.ViewModel
{
    public class TreeViewDragBehavior
    {
        private TreeView _tv;
        private Point _startPoint;

        public void Attach(TreeView tv)
        {
            _tv = tv;
            _tv.PreviewMouseLeftButtonDown += OnMouseDown;
            _tv.MouseMove += OnMouseMove;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            var diff = _startPoint - pos;
            if (System.Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                System.Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            if (_tv.SelectedItem is Model.NetworkEntity entity)
                DragDrop.DoDragDrop(_tv, entity.ID.ToString(), DragDropEffects.Move);
        }
    }

    public class SlotInteractionBehavior
    {
        private FrameworkElement _fe;
        private bool _dragStarted;
        private Point _startPoint;

        public void Attach(FrameworkElement fe)
        {
            _fe = fe;
            _fe.PreviewMouseLeftButtonDown += OnMouseDown;
            _fe.MouseMove += OnMouseMove;
            _fe.MouseLeftButtonUp += OnMouseUp;
            _fe.Drop += OnDrop;
            _fe.DragOver += OnDragOver;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStarted = false;
            _startPoint = e.GetPosition(null);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var slot = _fe.DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot?.Entity == null) return;
            var pos = e.GetPosition(null);
            var diff = _startPoint - pos;
            if (System.Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                System.Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _dragStarted = true;
            DragDrop.DoDragDrop(_fe, slot.Entity.ID.ToString(), DragDropEffects.Move);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragStarted) { _dragStarted = false; return; }
            var slot = _fe.DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot?.Entity == null) return;
            GetVM()?.ConnectOrDisconnect(slot.Entity.ID);
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            if (!int.TryParse((string)e.Data.GetData(DataFormats.StringFormat), out int id)) return;
            var vm = GetVM();
            var entity = vm?.GetEntityById(id);
            if (entity == null) return;
            var slot = _fe.DataContext as NetworkDisplayViewModel.CanvasSlot;
            if (slot == null) return;
            vm.PlaceEntity(slot, entity);
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat)
                ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private NetworkDisplayViewModel GetVM()
        {
            var fe = VisualTreeHelper.GetParent(_fe) as FrameworkElement;
            while (fe != null)
            {
                if (fe.DataContext is NetworkDisplayViewModel vm) return vm;
                fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
            }
            return null;
        }
    }

    public class TVAreaDropBehavior
    {
        private FrameworkElement _fe;

        public void Attach(FrameworkElement fe)
        {
            _fe = fe;
            _fe.Drop += OnDrop;
            _fe.DragOver += OnDragOver;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            if (!int.TryParse((string)e.Data.GetData(DataFormats.StringFormat), out int id)) return;
            var vm = _fe.DataContext as NetworkDisplayViewModel;
            var slot = vm?.CanvasSlots.FirstOrDefault(s => s.Entity?.ID == id);
            if (slot == null) return;
            vm.RemoveFromSlot(slot);
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat)
                ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }
    }

    public static class DragBehaviorAttacher
    {
        public static readonly DependencyProperty IsTVAreaDropProperty =
            DependencyProperty.RegisterAttached("IsTVAreaDrop", typeof(bool), typeof(DragBehaviorAttacher),
                new PropertyMetadata(false, OnIsTVAreaDropChanged));

        public static bool GetIsTVAreaDrop(DependencyObject obj) => (bool)obj.GetValue(IsTVAreaDropProperty);
        public static void SetIsTVAreaDrop(DependencyObject obj, bool value) => obj.SetValue(IsTVAreaDropProperty, value);

        private static void OnIsTVAreaDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && d is FrameworkElement fe)
                new TVAreaDropBehavior().Attach(fe);
        }

        public static readonly DependencyProperty IsTreeViewDragProperty =
            DependencyProperty.RegisterAttached("IsTreeViewDrag", typeof(bool), typeof(DragBehaviorAttacher),
                new PropertyMetadata(false, OnIsTreeViewDragChanged));

        public static bool GetIsTreeViewDrag(DependencyObject obj) => (bool)obj.GetValue(IsTreeViewDragProperty);
        public static void SetIsTreeViewDrag(DependencyObject obj, bool value) => obj.SetValue(IsTreeViewDragProperty, value);

        private static void OnIsTreeViewDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && d is TreeView tv)
                new TreeViewDragBehavior().Attach(tv);
        }

        public static readonly DependencyProperty IsSlotInteractionProperty =
            DependencyProperty.RegisterAttached("IsSlotInteraction", typeof(bool), typeof(DragBehaviorAttacher),
                new PropertyMetadata(false, OnIsSlotInteractionChanged));

        public static bool GetIsSlotInteraction(DependencyObject obj) => (bool)obj.GetValue(IsSlotInteractionProperty);
        public static void SetIsSlotInteraction(DependencyObject obj, bool value) => obj.SetValue(IsSlotInteractionProperty, value);

        private static void OnIsSlotInteractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && d is FrameworkElement fe)
                new SlotInteractionBehavior().Attach(fe);
        }
    }

    public static class ListViewDoubleClickBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ListViewDoubleClickBehavior),
                new PropertyMetadata(null, OnCommandChanged));

        public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);
        public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView lv)
            {
                lv.MouseDoubleClick -= OnDoubleClick;
                if (e.NewValue != null)
                    lv.MouseDoubleClick += OnDoubleClick;
            }
        }

        private static void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lv = (ListView)sender;
            var cmd = GetCommand(lv);
            if (cmd?.CanExecute(null) == true)
                cmd.Execute(null);
        }
    }
}
