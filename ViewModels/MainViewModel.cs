using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using workspace_hub.Commands;
using workspace_hub.Models;
using workspace_hub.Services;

namespace workspace_hub.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        private ICollectionView? _projectsView;
        public ICollectionView ProjectsView
        {
            get => _projectsView!;
            private set { _projectsView = value; RaisePropertyChanged(nameof(ProjectsView)); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) ProjectsView.Refresh(); }
        }

        private string _selectedStatus = "All";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { if (SetProperty(ref _selectedStatus, value)) ProjectsView.Refresh(); }
        }
        public MainViewModel()
        {
            // Load projects from storage
            var loaded = ProjectStorage.Load();
            foreach (var p in loaded) Projects.Add(p);

            ProjectsView = CollectionViewSource.GetDefaultView(Projects);
            ProjectsView.Filter = FilterProject;

            NewProjectCommand = new RelayCommand(_ => OnNewProject(), _ => true);
            // Expose an event so the View can open dialogs (the VM does not create windows)
            RequestNewProjectDialog = null;
        }

        private bool FilterProject(object obj)
        {
            if (obj is not Project project) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (project.Name == null || !project.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return string.Equals(SelectedStatus, "All", StringComparison.OrdinalIgnoreCase)
                || string.Equals(project.Status, SelectedStatus, StringComparison.OrdinalIgnoreCase);
        }

        public RelayCommand NewProjectCommand { get; }

        public void OnNewProject()
        {
            // The view will open the NewProjectWindow and add the created project into Projects collection.
            RequestNewProjectDialog?.Invoke();
        }

        public void SaveProjects()
        {
            ProjectStorage.Save(Projects);
        }

        // Event the View can subscribe to in order to open the NewProject dialog
        public Action? RequestNewProjectDialog { get; set; }
    }
}
