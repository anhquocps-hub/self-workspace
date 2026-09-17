using System;
using System.Windows;
using workspace_hub.Models;

namespace workspace_hub
{
    /// <summary>
    /// Interaction logic for EditProjectWindow.xaml
    /// </summary>
    public partial class EditProjectWindow : Window
    {
        private readonly Project _project;

        public EditProjectWindow(Project project)
        {
            InitializeComponent();
            _project = project ?? throw new ArgumentNullException(nameof(project));

            // Populate fields with existing values (work on copies in UI)
            NameTextBox.Text = _project.Name;
            NoteTextBox.Text = _project.Note;
            DeadlinePicker.SelectedDate = _project.Deadline;

            // Select status in ComboBox
            for (int i = 0; i < StatusComboBox.Items.Count; i++)
            {
                if (StatusComboBox.Items[i] is System.Windows.Controls.ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), _project.Status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                System.Windows.MessageBox.Show("Project name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var statusItem = StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var status = statusItem?.Content?.ToString() ?? "Todo";

            // Update existing project properties (safe assignment)
            _project.Name = name;
            _project.Note = NoteTextBox.Text ?? string.Empty;
            _project.Status = status;
            _project.Deadline = DeadlinePicker.SelectedDate;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
