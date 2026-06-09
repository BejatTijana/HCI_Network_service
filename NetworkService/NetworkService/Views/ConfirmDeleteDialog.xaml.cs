using System.Windows;

namespace NetworkService.Views
{
    public partial class ConfirmDeleteDialog : Window
    {
        public ConfirmDeleteDialog(string entityName)
        {
            InitializeComponent();
            MessageText.Text = $"Delete \"{entityName}\"?\nYou can undo this action.";
        }

        private void Delete_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
