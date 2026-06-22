using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NetworkService.Controls
{
    public partial class VirtualKeyboardControl : UserControl
    {
        public static readonly DependencyProperty TargetTextBoxProperty =
            DependencyProperty.Register("TargetTextBox", typeof(TextBox),
                typeof(VirtualKeyboardControl), new PropertyMetadata(null));

        public TextBox TargetTextBox
        {
            get => (TextBox)GetValue(TargetTextBoxProperty);
            set => SetValue(TargetTextBoxProperty, value);
        }

        private bool _isShifted = true;

        private static readonly SolidColorBrush ShiftActive
            = new SolidColorBrush(Color.FromRgb(0xEC, 0xF0, 0xF1));
        private static readonly SolidColorBrush ShiftInactive
            = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6));

        public VirtualKeyboardControl()
        {
            InitializeComponent();
            Loaded += (s, e) => ShiftButton.Foreground = ShiftActive;
        }

        private void OnKeyClick(object sender, RoutedEventArgs e)
        {
            var tb = TargetTextBox;
            if (tb == null) return;
            string key = ((Button)sender).Content.ToString();
            int caret = tb.CaretIndex;
            if (key == "⌫")
            {
                if (caret > 0) { tb.Text = tb.Text.Remove(caret - 1, 1); tb.CaretIndex = caret - 1; }
            }
            else if (key == "space")
            {
                tb.Text = tb.Text.Insert(caret, " "); tb.CaretIndex = caret + 1;
            }
            else if (key == "↵") { }
            else if (sender == ShiftButton)
            {
                _isShifted = !_isShifted;
                ShiftButton.Foreground = _isShifted ? ShiftActive : ShiftInactive;
            }
            else if (key.Length == 1 && char.IsLetter(key[0]))
            {
                tb.Text = tb.Text.Insert(caret, _isShifted ? key.ToUpper() : key.ToLower());
                tb.CaretIndex = caret + 1;
                _isShifted = false;
                ShiftButton.Foreground = ShiftInactive;
            }
            else
            {
                tb.Text = tb.Text.Insert(caret, key);
                tb.CaretIndex = caret + 1;
            }
        }
    }
}
