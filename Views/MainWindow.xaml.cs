using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using workspace_hub.ViewModels;
using workspace_hub.Models;
using workspace_hub.Services;


namespace workspace_hub.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        public ObservableCollection<Project> Projects => _viewModel!.Projects;
        public ICollectionView ProjectsView => _viewModel!.ProjectsView;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _viewModel.RequestNewProjectDialog = () =>
            {
                var dlg = new NewProjectWindow { Owner = this };
                if (dlg.ShowDialog() == true && dlg.CreatedProject != null)
                {
                    Projects.Add(dlg.CreatedProject);
                    _viewModel.SaveProjects();
                    RefreshViewState();
                }
            };

            // Keep the empty state synchronized when search or status filters change.
            ProjectsView.CollectionChanged += (_, _) => UpdateViewState();
            RefreshViewState();
            CheckDeadlinesAndNotify();
        }
        // Simple in-memory set to avoid repeating notifications during this session
        private readonly System.Collections.Generic.HashSet<string> _notifiedThisSession = new System.Collections.Generic.HashSet<string>();

        private void CheckDeadlinesAndNotify()
        {
            // Compute counts and fire brief notifications for Due Today and Overdue projects
            foreach (var project in Projects)
            {
                var state = GetDeadlineState(project);
                if (state == DeadlineState.DueToday || state == DeadlineState.Overdue)
                {
                    var key = $"{project.Id}:{state}";
                    if (!_notifiedThisSession.Contains(key))
                    {
                        _notifiedThisSession.Add(key);
                        // Show a simple balloon/notification - use Toast where possible but fall back to MessageBox
                        TryShowNotification(project, state);
                    }
                }
            }
        }

        private enum DeadlineState { Normal, DueSoon, DueToday, DueTomorrow, Overdue, Completed }

        private DeadlineState GetDeadlineState(Project project)
        {
            if (project == null || project.Deadline == null) return DeadlineState.Normal;

            if (string.Equals(project.Status ?? string.Empty, "Done", StringComparison.OrdinalIgnoreCase))
                return DeadlineState.Completed;

            var now = DateTime.Now.Date;
            var deadline = project.Deadline.Value.Date;
            if (deadline < now) return DeadlineState.Overdue;
            if (deadline == now) return DeadlineState.DueToday;
            if (deadline == now.AddDays(1)) return DeadlineState.DueTomorrow;
            if (deadline <= now.AddDays(3)) return DeadlineState.DueSoon;
            return DeadlineState.Normal;
        }

        private void TryShowNotification(Project project, DeadlineState state)
        {
            // Use a simple NotificationWindow approach: try using Toasts would require packaging; to keep simple, use a non-blocking MessageBox replacement (Notification via System.Windows.Forms.NotifyIcon)
            try
            {
                var title = "Workspace Hub";
                string? text = null;
                if (state == DeadlineState.DueToday)
                {
                    text = $"\"{project.Name}\" is due today.";
                }
                else if (state == DeadlineState.Overdue)
                {
                    var days = (DateTime.Now.Date - project.Deadline.GetValueOrDefault().Date).Days;
                    text = $"\"{project.Name}\" is overdue by {days} day{(days == 1 ? "" : "s")}.";
                }

                if (string.IsNullOrEmpty(text)) return;

                // Use NotifyIcon balloon tip if available to avoid modal message boxes
                ShowBalloonNotification(title, text);
            }
            catch
            {
                // Swallow notification errors; they are not critical
            }
        }

        private void ShowBalloonNotification(string title, string text)
        {
            // Create a NotifyIcon for the brief display; keep it in-memory and dispose after showing
            var ni = new System.Windows.Forms.NotifyIcon();
            ni.Visible = true;
            ni.Icon = System.Drawing.SystemIcons.Information;
            ni.BalloonTipTitle = title;
            ni.BalloonTipText = text;
            ni.ShowBalloonTip(4000);
            // Dispose after a short delay
            var t = new System.Timers.Timer(4500) { AutoReset = false };
            t.Elapsed += (s, e) => { ni.Dispose(); t.Dispose(); };
            t.Start();
        }

        private void StatusFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.SelectedStatus = sender == TodoFilterButton ? "Todo"
                : sender == InProgressFilterButton ? "In Progress"
                : sender == DoneFilterButton ? "Done" : "All";
            RefreshViewState();
        }
        private void ProjectCard_EditRequested(object? sender, Project project)
        {
            if (project == null) return;

            // View handles edit dialog, then notify ViewModel to persist
            var dlg = new EditProjectWindow(project) { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true)
            {
                // Project properties were updated directly via binding/code-behind
                _viewModel!.SaveProjects();
                ProjectsView.Refresh();
                RefreshViewState();
            }
        }

        private void ProjectCard_DeleteRequested(object? sender, Project project)
        {
            if (project == null) return;

            var msg = $"Are you sure you want to delete \"{project.Name}\"?\n\nThis will remove the project from Workspace Hub but will NOT delete files on disk.";
            var res = System.Windows.MessageBox.Show(msg, "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                Projects.Remove(project);
                _viewModel!.SaveProjects();
                ProjectsView.Refresh();
                RefreshViewState();
            }
        }

        private void ProjectCard_ProjectChanged(object? sender, Project project)
        {
            if (project == null) return;

            try
            {
                // Persist when Project content (folders, primary) changes
                _viewModel!.SaveProjects();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save project changes:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                ProjectsView.Refresh();
            }
            catch { /* ignore view refresh errors */ }

            try
            {
                RefreshViewState();
            }
            catch { /* ignore UI refresh errors */ }
        }

        // Update counts for status filter buttons
        private void UpdateStatusCounts()
        {
            var total = Projects.Count;
            var todo = 0;
            var inprog = 0;
            var done = 0;
            foreach (var p in Projects)
            {
                var s = (p.Status ?? string.Empty).Trim();
                if (string.Equals(s, "Todo", StringComparison.OrdinalIgnoreCase)) todo++;
                else if (string.Equals(s, "In Progress", StringComparison.OrdinalIgnoreCase)) inprog++;
                else if (string.Equals(s, "Done", StringComparison.OrdinalIgnoreCase)) done++;
            }

            if (AllFilterButton != null) AllFilterButton.Content = $"All ({total})";
            if (TodoFilterButton != null) TodoFilterButton.Content = $"Todo ({todo})";
            if (InProgressFilterButton != null) InProgressFilterButton.Content = $"In Progress ({inprog})";
            if (DoneFilterButton != null) DoneFilterButton.Content = $"Done ({done})";
        }

        private void SortComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;

            ProjectsView.SortDescriptions.Clear();
            var item = SortComboBox?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var val = item?.Content?.ToString();
            if (string.Equals(val, "Name A-Z", StringComparison.OrdinalIgnoreCase))
            {
                ProjectsView.SortDescriptions.Add(new SortDescription(nameof(Project.Name), ListSortDirection.Ascending));
            }
            else if (string.Equals(val, "Name Z-A", StringComparison.OrdinalIgnoreCase))
            {
                ProjectsView.SortDescriptions.Add(new SortDescription(nameof(Project.Name), ListSortDirection.Descending));
            }
            else if (string.Equals(val, "Deadline Earliest", StringComparison.OrdinalIgnoreCase))
            {
                ProjectsView.SortDescriptions.Add(new SortDescription(nameof(Project.Deadline), ListSortDirection.Ascending));
            }
            else if (string.Equals(val, "Deadline Latest", StringComparison.OrdinalIgnoreCase))
            {
                ProjectsView.SortDescriptions.Add(new SortDescription(nameof(Project.Deadline), ListSortDirection.Descending));
            }

            RefreshViewState();
        }

        private void RefreshViewState()
        {
            if (_viewModel == null) return;
            // Ensure view is refreshed
            ProjectsView.Refresh();

            UpdateViewState();
        }

        private void UpdateViewState()
        {
            if (_viewModel == null) return;
            // Update empty state text
            if (EmptyStateTextBlock != null)
            {
                if (Projects.Count == 0)
                {
                    EmptyStateTextBlock.Text = "No projects yet. Create your first project.";
                    EmptyStateTextBlock.Visibility = Visibility.Visible;
                }
                else if (ProjectsView.IsEmpty)
                {
                    EmptyStateTextBlock.Text = "No projects match your search or filters.";
                    EmptyStateTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    EmptyStateTextBlock.Visibility = Visibility.Collapsed;
                }
            }

            // Update status counts whenever view state refreshes
            UpdateStatusCounts();
        }

        private void ProjectCard_OpenRequested(object sender, Project project)
        {
            if (project == null)
            {
                System.Windows.MessageBox.Show("Project is null.", "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var folder = project.PrimaryFolder;

            if (string.IsNullOrWhiteSpace(folder))
            {
                var url = ProjectLinks.FirstUrl(project);
                if (url == null)
                {
                    System.Windows.MessageBox.Show("Please add a folder or a valid HTTP/HTTPS URL.", "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to open URL:\n{ex.Message}", "Open Project", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }
            if (!Directory.Exists(folder))
            {
                System.Windows.MessageBox.Show($"Folder not found:\n{folder}", "Folder not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Use shell execute to open the folder in File Explorer
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
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
}