using System.Windows;
using System.Windows.Controls;

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

        public VirtualKeyboardControl() { InitializeComponent(); }

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
            else if (key == "↵" || key == "123") { }
            else { tb.Text = tb.Text.Insert(caret, key); tb.CaretIndex = caret + 1; }
        }
    }
}
