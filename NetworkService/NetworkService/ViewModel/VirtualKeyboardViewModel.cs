using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NetworkService.Commands;

namespace NetworkService.ViewModel
{
    public class VirtualKeyboardViewModel : INotifyPropertyChanged
    {
        private static readonly SolidColorBrush ShiftActive
            = new SolidColorBrush(Color.FromRgb(0xEC, 0xF0, 0xF1));
        private static readonly SolidColorBrush ShiftInactive
            = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));

        private bool _isShifted = true;

        public TextBox TargetTextBox { get; set; }

        public bool IsShifted
        {
            get => _isShifted;
            set { _isShifted = value; OnPropertyChanged(nameof(IsShifted)); OnPropertyChanged(nameof(ShiftForeground)); }
        }

        public SolidColorBrush ShiftForeground => _isShifted ? ShiftActive : ShiftInactive;

        public ICommand KeyPressCommand { get; }

        public VirtualKeyboardViewModel()
        {
            KeyPressCommand = new RelayCommand(param => OnKeyPress(param?.ToString() ?? ""));
        }

        private void OnKeyPress(string key)
        {
            var tb = TargetTextBox;
            if (tb == null) return;
            int caret = tb.CaretIndex;
            if (key == "⌫")
            {
                if (caret > 0) { tb.Text = tb.Text.Remove(caret - 1, 1); tb.CaretIndex = caret - 1; }
            }
            else if (key == "space")
            {
                tb.Text = tb.Text.Insert(caret, " "); tb.CaretIndex = caret + 1;
            }
            else if (key == "SHIFT")
            {
                IsShifted = !IsShifted;
            }
            else if (key.Length == 1 && char.IsLetter(key[0]))
            {
                tb.Text = tb.Text.Insert(caret, IsShifted ? key.ToUpper() : key.ToLower());
                tb.CaretIndex = caret + 1;
                IsShifted = false;
            }
            else
            {
                tb.Text = tb.Text.Insert(caret, key);
                tb.CaretIndex = caret + 1;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static class VirtualKeyboardAttacher
    {
        public static readonly System.Windows.DependencyProperty TargetTextBoxProperty =
            System.Windows.DependencyProperty.RegisterAttached(
                "TargetTextBox", typeof(TextBox), typeof(VirtualKeyboardAttacher),
                new System.Windows.PropertyMetadata(null, OnTargetTextBoxChanged));

        public static TextBox GetTargetTextBox(System.Windows.DependencyObject obj)
            => (TextBox)obj.GetValue(TargetTextBoxProperty);
        public static void SetTargetTextBox(System.Windows.DependencyObject obj, TextBox value)
            => obj.SetValue(TargetTextBoxProperty, value);

        private static void OnTargetTextBoxChanged(System.Windows.DependencyObject d,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.FrameworkElement fe)
            {
                void Apply()
                {
                    if (fe.DataContext is VirtualKeyboardViewModel vm)
                        vm.TargetTextBox = e.NewValue as TextBox;
                }
                if (fe.IsLoaded) Apply();
                else fe.Loaded += (s, _) => Apply();
            }
        }
    }
}
