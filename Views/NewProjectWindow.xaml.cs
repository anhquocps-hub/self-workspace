using System;
using System.Linq;
using System.Windows;
using workspace_hub.Models;
using workspace_hub.Services;

namespace workspace_hub.Views
{
    /// <summary>
    /// Interaction logic for NewProjectWindow.xaml
    /// </summary>
    public partial class NewProjectWindow : Window
    {
        public Project? CreatedProject { get; private set; }

        public NewProjectWindow()
        {
            InitializeComponent();
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select project folder";
            dialog.UseDescriptionForTitle = true;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void ClearFolder_Click(object sender, RoutedEventArgs e) => FolderPathTextBox.Clear();

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {

            var name = NameTextBox.Text?.Trim();
            var folder = FolderPathTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                System.Windows.MessageBox.Show("Project name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ProjectLinksEditor.CommitPending()) return;

            if (!ProjectLinks.HasLocation(new[] { folder ?? "" }, ProjectLinksEditor.Links))
            {
                System.Windows.MessageBox.Show("Please select a folder or add a valid HTTP/HTTPS URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(folder) && !System.IO.Directory.Exists(folder))
            {
                var res = System.Windows.MessageBox.Show($"The selected folder does not exist:\n{folder}\n\nDo you want to continue and add the project without creating the folder?", "Folder not found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var statusItem = StatusComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var status = statusItem?.Content?.ToString() ?? "Todo";

            var project = new Project
            {
                Name = name,
                Note = NoteTextBox.Text ?? string.Empty,
                Status = status,
                Deadline = DeadlinePicker.SelectedDate
            };

            if (!string.IsNullOrWhiteSpace(folder))
            {
                project.FolderPaths.Add(folder);
                project.PrimaryFolderPath = folder;
            }
            ProjectLinks.UpdateUrls(project, ProjectLinksEditor.Links);

            CreatedProject = project;
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
