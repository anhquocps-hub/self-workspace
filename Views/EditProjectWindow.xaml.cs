using System;
using System.Windows;
using workspace_hub.Models;
using workspace_hub.Services;

namespace workspace_hub.Views
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

            foreach (var url in _project.MediaPaths.Where(ProjectLinks.IsWebUrl))
                if (!ProjectLinksEditor.Links.Contains(url.Trim())) ProjectLinksEditor.Links.Add(url.Trim());

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

            if (!ProjectLinksEditor.CommitPending()) return;
            if (!ProjectLinks.HasLocation(_project.FolderPaths, ProjectLinksEditor.Links))
            {
                System.Windows.MessageBox.Show("Please keep a folder or add a valid HTTP/HTTPS URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var statusItem = StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var status = statusItem?.Content?.ToString() ?? "Todo";

            // Update existing project properties (safe assignment)
            _project.Name = name;
            _project.Note = NoteTextBox.Text ?? string.Empty;
            _project.Status = status;
            _project.Deadline = DeadlinePicker.SelectedDate;

            ProjectLinks.UpdateUrls(_project, ProjectLinksEditor.Links);

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
