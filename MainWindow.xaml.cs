using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using workspace_hub.Models;
using workspace_hub.Services;

namespace workspace_hub
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public ICollectionView ProjectsView { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // Load persisted projects (if any)
            var loaded = ProjectStorage.Load();
            Projects.Clear();
            foreach (var p in loaded)
            {
                Projects.Add(p);
            }

            // Initialize view over Projects
            ProjectsView = CollectionViewSource.GetDefaultView(Projects);
            ProjectsView.Filter = FilterProject;

            // Set a MainViewModel as DataContext
            var vm = new ViewModels.MainViewModel();
            DataContext = vm;

            // Subscribe to ViewModel requests for dialogs
            vm.RequestNewProjectDialog = () =>
            {
                var dlg = new NewProjectWindow { Owner = this };
                var result = dlg.ShowDialog();
                if (result == true && dlg.CreatedProject != null)
                {
                    vm.Projects.Add(dlg.CreatedProject);
                    vm.SaveProjects();
                    // refresh view state via existing methods
                    ProjectsView = vm.ProjectsView;
                    RefreshViewState();
                }
            };

            // Wire ProjectsView for existing UI bindings
            ProjectsView = vm.ProjectsView;

            // initial empty-state update
            RefreshViewState();

            // After loading projects, check deadlines and send initial notifications
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
                    var days = (DateTime.Now.Date - project.Deadline.Value.Date).Days;
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
            if (sender is System.Windows.Controls.Button btn)
            {
                var txt = btn.Content?.ToString() ?? "All";
                // Content is like "Todo (3)" - extract prefix before space
                var parts = txt.Split(' ');
                var key = parts[0];
                // Map 'In' from 'In Progress' case
                if (txt.StartsWith("In Progress", StringComparison.OrdinalIgnoreCase))
                    key = "In Progress";

                // Find matching ComboBoxItem and select it
                foreach (var item in StatusComboBox.Items)
                {
                    if (item is System.Windows.Controls.ComboBoxItem cbi)
                    {
                        var content = cbi.Content?.ToString();
                        if (string.Equals(content, key, StringComparison.OrdinalIgnoreCase))
                        {
                            StatusComboBox.SelectedItem = cbi;
                            break;
                        }
                    }
                }
            }
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // Keep compatibility for views that still call the old handler (should be replaced by command)
            var dlg = new NewProjectWindow { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true && dlg.CreatedProject != null)
            {
                Projects.Add(dlg.CreatedProject);
                // Persist after create
                ProjectStorage.Save(Projects);
                ProjectsView.Refresh();
                RefreshViewState();
            }
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
                ProjectStorage.Save(Projects);
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
                // Delegate delete to ViewModel collection if available
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.Projects.Remove(project);
                    vm.SaveProjects();
                    ProjectsView = vm.ProjectsView;
                }
                else
                {
                    Projects.Remove(project);
                    ProjectStorage.Save(Projects);
                }
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
                ProjectStorage.Save(Projects);
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

        private bool FilterProject(object obj)
        {
            if (obj is not Project project) return false;
            // Delegate to ViewModel's filter if available
            if (DataContext is ViewModels.MainViewModel vm)
            {
                // Use vm.SearchText
                var search = vm.SearchText?.Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    if (project.Name == null || !project.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                // Status filter via StatusComboBox (hidden) still used in view; keep compatibility
                var statusItem = StatusComboBox?.SelectedItem as System.Windows.Controls.ComboBoxItem;
                var status = statusItem?.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(project.Status ?? string.Empty, status, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            else
            {
                // Fallback to old behavior: SearchTextBox
                var search = SearchTextBox?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    if (project.Name == null || !project.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ProjectsView.Refresh();
            RefreshViewState();
        }

        private void StatusComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ProjectsView.Refresh();
            RefreshViewState();
        }

        private void SortComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProjectsView == null) return;

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
            if (ProjectsView == null) return;
            // Ensure view is refreshed
            ProjectsView.Refresh();

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
                var displayName = project?.Name ?? "(unknown)";
                System.Windows.MessageBox.Show($"No primary folder is set for project '{displayName}'.", "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
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
