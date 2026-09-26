using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using workspace_hub.Models;
using WinForms = System.Windows.Forms;
using MediaBrush = System.Windows.Media.Brush;

namespace workspace_hub.Controls
{
    public partial class ProjectDetails : ProjectControl
    {
        // 1. Khởi tạo ProjectDetails
        public ProjectDetails()
        {
            InitializeComponent();
        }

        public event EventHandler? CloseRequested;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        // 10. Khi nhấn nút "Delete"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Project != null)
            {
                RequestDelete();
            }
        }

        // 9. Khi nhấn nút "Edit"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (Project != null)
            {
                RequestEdit();
            }
        }

        // 8. Open a specific folder from the folder list
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                var path = btn.Tag as string ?? btn.DataContext as string;
                if (string.IsNullOrWhiteSpace(path))
                {
                    System.Windows.MessageBox.Show("Folder path is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Directory.Exists(path))
                {
                    System.Windows.MessageBox.Show($"Folder not found:\n{path}", "Folder not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception wex)
                {
                    System.Windows.MessageBox.Show($"Failed to open folder. The operating system could not start the specified program.\n{wex.Message}", "Open Folder", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to open folder:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetPrimary_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null) return;
            if (sender is System.Windows.Controls.Button btn)
            {
                var path = btn.Tag as string ?? btn.DataContext as string;
                if (string.IsNullOrWhiteSpace(path)) return;

                if (!Project.FolderPaths.Contains(path)) return;

                Project.PrimaryFolderPath = path;
                NotifyChanged();
            }
        }


        private void Status_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (e.Property == System.Windows.Controls.Primitives.Selector.SelectedValueProperty
                && sender is System.Windows.Controls.ComboBox combo
                && combo.SelectedValue is string)
                NotifyChanged();
        }
        // 6. Khi nhấn nút "Open Workspace"
        private void OpenButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project != null)
            {
                RequestOpen();
            }
        }

        // 7. Khi nhấn nút "Add Folder"
        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Project == null)
            {
                System.Windows.MessageBox.Show("Project is not set.");
                return;
            }

            using var dialog = new WinForms.FolderBrowserDialog();
            dialog.Description = "Select a folder for this project";
            dialog.UseDescriptionForTitle = true;

            var result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                var path = dialog.SelectedPath;
                if (!Project.FolderPaths.Contains(path))
                {
                    Project.FolderPaths.Add(path);
                    // Notify user briefly
                    System.Windows.MessageBox.Show($"Added folder:\n{path}", "Folder added", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Notify parent that project changed so it can persist
                    NotifyChanged();
                }
                else
                {
                    System.Windows.MessageBox.Show("This folder is already added for the project.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Notify parent when project data changed (folders, primary folder, etc.)


        // Open media (file path or http/https url)
        private void OpenMedia_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                var target = btn.Tag as string ?? btn.DataContext as string;
                if (string.IsNullOrWhiteSpace(target))
                {
                    System.Windows.MessageBox.Show("Media path is empty.", "Open Media", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // If it's an HTTP/HTTPS URL, open in default browser
                if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to open URL:\n{ex.Message}", "Open Media", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // Otherwise treat as local file
                try
                {
                    if (!System.IO.File.Exists(target))
                    {
                        System.Windows.MessageBox.Show($"File not found:\n{target}", "Open Media", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception wex)
                {
                    System.Windows.MessageBox.Show($"Failed to open media. The system could not start the associated program.\n{wex.Message}", "Open Media", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to open media:\n{ex.Message}", "Open Media", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}