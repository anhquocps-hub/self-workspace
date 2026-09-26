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
        private void ProjectCard_DetailsRequested(object? sender, Project project)
        {
            var popup = new ProjectDetailsWindow(project) { Owner = this };
            popup.DetailsContent.OpenRequested += ProjectCard_OpenRequested;
            popup.DetailsContent.EditRequested += ProjectCard_EditRequested;
            popup.DetailsContent.DeleteRequested += ProjectCard_DeleteRequested;
            popup.DetailsContent.ProjectChanged += ProjectCard_ProjectChanged;
            try
            {
                popup.ShowDialog();
            }
            finally
            {
                popup.DetailsContent.OpenRequested -= ProjectCard_OpenRequested;
                popup.DetailsContent.EditRequested -= ProjectCard_EditRequested;
                popup.DetailsContent.DeleteRequested -= ProjectCard_DeleteRequested;
                popup.DetailsContent.ProjectChanged -= ProjectCard_ProjectChanged;
            }
        }

        private Window ActionOwner(object? sender) =>
            sender is DependencyObject element ? Window.GetWindow(element) ?? this : this;
        private void ProjectCard_EditRequested(object? sender, Project project)
        {
            if (project == null) return;

            // View handles edit dialog, then notify ViewModel to persist
            var dlg = new EditProjectWindow(project) { Owner = ActionOwner(sender) };
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
            var res = System.Windows.MessageBox.Show(ActionOwner(sender), msg, "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                if (!Projects.Remove(project)) return;
                _viewModel!.SaveProjects();
                ProjectsView.Refresh();
                RefreshViewState();
                if (ActionOwner(sender) is ProjectDetailsWindow popup) popup.Close();
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

        private void ProjectCard_OpenRequested(object? sender, Project project)
        {
            if (project == null)
            {
                System.Windows.MessageBox.Show("Project is null.", "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var errors = new System.Collections.Generic.List<string>();
            var seenPaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUrls = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var resources = new System.Collections.Generic.List<(string Target, bool IsFolder, bool IsUrl)>();

            void AddResource(string? value, bool isFolder)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var target = value.Trim();
                var isUrl = !isFolder && ProjectLinks.IsWebUrl(target);
                var key = target;
                if (!isUrl)
                {
                    try
                    {
                        key = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
                    }
                    catch (Exception)
                    {
                        // Keep invalid paths in the count and report their errors when opening.
                    }
                }
                if (!(isUrl ? seenUrls : seenPaths).Add(key)) return;
                resources.Add((target, isFolder, isUrl));
            }

            foreach (var folder in project.FolderPaths) AddResource(folder, isFolder: true);
            foreach (var media in project.MediaPaths) AddResource(media, isFolder: false);

            if (resources.Count == 0)
            {
                System.Windows.MessageBox.Show("Please add a folder, a valid HTTP/HTTPS URL, or a media file.", "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var folderCount = resources.Count(resource => resource.IsFolder);
            var linkCount = resources.Count(resource => resource.IsUrl);
            var fileCount = resources.Count - folderCount - linkCount;
            if (linkCount > 5 || folderCount > 3 || fileCount > 2)
            {
                var result = System.Windows.MessageBox.Show(
                    $"You are about to open {linkCount} links, {folderCount} folders, and {fileCount} media files.\n\nOpening many resources may slow down your computer. Continue?",
                    "Open Project", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result != MessageBoxResult.Yes) return;
            }

            foreach (var (target, isFolder, isUrl) in resources)
            {
                try
                {
                    if (!isUrl)
                    {
                        if (isFolder ? !Directory.Exists(target) : !File.Exists(target))
                        {
                            errors.Add($"{(isFolder ? "Folder" : "File")} not found: {target}");
                            continue;
                        }
                    }

                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to open {target}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                System.Windows.MessageBox.Show("Some resources could not be opened:\n\n" + string.Join("\n", errors), "Open Project", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
