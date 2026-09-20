using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace workspace_hub.Models
{
    // Simple Project model with change notification for PrimaryFolderPath/PrimaryFolder
    public class Project : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // constructor to hook collection changes
        public Project()
        {
            FolderPaths.CollectionChanged += (s, e) =>
            {
                // Folder list changed; PrimaryFolder may be affected
                OnPropertyChanged(nameof(PrimaryFolder));
            };
        }

        private string? _primaryFolderPath;

        public string? PrimaryFolderPath
        {
            get => _primaryFolderPath;
            set
            {
                if (_primaryFolderPath != value)
                {
                    _primaryFolderPath = value;
                    OnPropertyChanged(nameof(PrimaryFolderPath));
                    OnPropertyChanged(nameof(PrimaryFolder));
                }
            }
        }

        // PrimaryFolder returns PrimaryFolderPath if valid, otherwise falls back to first folder
        public string? PrimaryFolder
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PrimaryFolderPath) && FolderPaths.Contains(PrimaryFolderPath))
                    return PrimaryFolderPath;
                return FolderPaths.FirstOrDefault();
            }
        }

        // rest of properties...

        public int Id { get; set; }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private string _note = "";
        public string Note
        {
            get => _note;
            set
            {
                if (_note != value)
                {
                    _note = value;
                    OnPropertyChanged(nameof(Note));
                }
            }
        }

        private DateTime? _deadline;
        public DateTime? Deadline
        {
            get => _deadline;
            set
            {
                if (_deadline != value)
                {
                    _deadline = value;
                    OnPropertyChanged(nameof(Deadline));
                }
            }
        }

        // Multiple folder paths for a project (optional). The first entry can be used as primary.
        // Use ObservableCollection so the UI can observe additions automatically.
        public ObservableCollection<string> FolderPaths { get; set; } = new ObservableCollection<string>();

        // Media paths or URLs (images, videos, links). Support local file paths and http/https URLs.
        public ObservableCollection<string> MediaPaths { get; set; } = new ObservableCollection<string>();

        // Convenience property for compatibility
        // PrimaryFolderAlready provided above
    }
}
