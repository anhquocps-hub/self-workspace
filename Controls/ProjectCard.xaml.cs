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
    public partial class ProjectCard : System.Windows.Controls.UserControl
    {
        // 1. Khởi tạo ProjectCard
        public ProjectCard()
        {
            InitializeComponent();
        }

        // 10. Khi nhấn nút "Delete"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Project != null)
            {
                DeleteRequested?.Invoke(this, Project);
            }
        }

        // 9. Khi nhấn nút "Edit"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (Project != null)
            {
                EditRequested?.Invoke(this, Project);
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
                ProjectChanged?.Invoke(this, Project);
            }
        }


        // 2. Đăng ký thuộc tính Project với WPF
        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(
                "Project",                  // Tên property
                typeof(Project),            // Kiểu dữ liệu
                typeof(ProjectCard),        // Property thuộc ProjectCard
                new PropertyMetadata(
                    null,
                    OnProjectChanged
                )
            );


        // 3. Property để nhận Project từ bên ngoài
        public Project? Project
        {
            get
            {
                return (Project?)GetValue(ProjectProperty);
            }

            set
            {
                SetValue(ProjectProperty, value);
            }
        }


        // 4. Khi Project thay đổi
        private static void OnProjectChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ProjectCard card = (ProjectCard)d;
            // Cho XAML sử dụng:
            // {Binding Name}
            // {Binding Status}
            // {Binding Note}
            card.DataContext = e.NewValue;

            // Initialize status ComboBox selection when project changes
            var cb = card.FindName("StatusComboBox") as System.Windows.Controls.ComboBox;
            if (cb != null && e.NewValue is Project p)
            {
                // SelectedValuePath is Content, so set SelectedValue to the status string
                cb.SelectedValue = p.Status;
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Project == null) return;
            if (sender is System.Windows.Controls.ComboBox cb)
            {
                var selected = cb.SelectedValue as string ?? (cb.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    Project.Status = selected;
                    // notify parent to persist and update filters/counts
                    ProjectChanged?.Invoke(this, Project);
                }
            }
        }


        // 5. Event báo ra ngoài khi người dùng muốn mở Project
        public event EventHandler<Project>? OpenRequested;
        public event EventHandler<Project>? EditRequested;
        public event EventHandler<Project>? DeleteRequested;


        // 6. Khi nhấn nút "Open Workspace"
        private void OpenButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Project != null)
            {
                OpenRequested?.Invoke(this, Project);
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
                    ProjectChanged?.Invoke(this, Project);
                }
                else
                {
                    System.Windows.MessageBox.Show("This folder is already added for the project.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Notify parent when project data changed (folders, primary folder, etc.)
        public event EventHandler<Project>? ProjectChanged;

        // Track notified states in-memory for this session to avoid duplicate notifications
        private static readonly System.Collections.Generic.HashSet<string> _notified = new System.Collections.Generic.HashSet<string>();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateDeadlineStateDisplay();
            // subscribe to Project property changes to update deadline state live
            if (Project != null)
            {
                Project.PropertyChanged -= Project_PropertyChanged;
                Project.PropertyChanged += Project_PropertyChanged;
            }
        }

        private void Project_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Project.Deadline) || e.PropertyName == nameof(Project.Status))
            {
                UpdateDeadlineStateDisplay();
            }
        }

        private void UpdateDeadlineStateDisplay()
        {
            // Find the DeadlineStateText TextBlock by name
            var tb = this.FindName("DeadlineStateText") as System.Windows.Controls.TextBlock;
            if (tb == null) return;

            if (Project == null || Project.Deadline == null)
            {
                tb.Text = string.Empty;
                tb.Foreground = (MediaBrush)FindResource("DeadlineNormalBrush");
                return;
            }

            var now = DateTime.Now.Date;
            var deadline = Project.Deadline.Value.Date;

            // If completed, show Completed
            if (string.Equals(Project.Status ?? string.Empty, "Done", StringComparison.OrdinalIgnoreCase))
            {
                tb.Text = "Completed";
                tb.Foreground = (MediaBrush)FindResource("DeadlineCompletedBrush");
                return;
            }

            if (deadline < now)
            {
                var days = (now - deadline).Days;
                tb.Text = days == 1 ? "Overdue by 1 day" : $"Overdue by {days} days";
                tb.Foreground = (MediaBrush)FindResource("DeadlineOverdueBrush");
            }
            else if (deadline == now)
            {
                tb.Text = "Due today";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else if (deadline == now.AddDays(1))
            {
                tb.Text = "Due tomorrow";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else if (deadline <= now.AddDays(3))
            {
                var days = (deadline - now).Days;
                tb.Text = days == 1 ? "Due in 1 day" : $"Due in {days} days";
                tb.Foreground = (MediaBrush)FindResource("DeadlineSoonBrush");
            }
            else
            {
                tb.Text = string.Empty;
                tb.Foreground = (MediaBrush)FindResource("DeadlineNormalBrush");
            }
        }
    }
}