using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using workspace_hub.Models;

namespace workspace_hub.Services
{
    public static class ProjectStorage
    {
        private static string GetFolder()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(local, "WorkspaceHub");
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            return appDir;
        }

        private static string GetFilePath() => Path.Combine(GetFolder(), "projects.json");

        public static void Save(System.Collections.Generic.IEnumerable<Project> projects)
        {
            var path = GetFilePath();
            var tempPath = path + ".tmp";
            var backupPath = Path.ChangeExtension(path, ".backup.json");
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var list = projects.Select(p => p).ToList();
                var json = JsonSerializer.Serialize(list, options);

                // Write to a temporary file first to avoid leaving a partial file on failure
                File.WriteAllText(tempPath, json);

                // If the target exists, replace it while keeping a small backup; otherwise move the temp file
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
                    }
                    catch
                    {
                        // Fallback: try copy then delete
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (UnauthorizedAccessException ua)
            {
                System.Windows.MessageBox.Show($"Workspace Hub could not save your projects. Access denied to the data folder.\n{ua.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException io)
            {
                System.Windows.MessageBox.Show($"Workspace Hub could not save your projects due to an IO error.\n{io.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Workspace Hub failed to save projects:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static ObservableCollection<Project> Load()
        {
            try
            {
                var path = GetFilePath();
                if (!File.Exists(path)) return new ObservableCollection<Project>();

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return new ObservableCollection<Project>();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<System.Collections.Generic.List<Project>>(json, options);
                if (list == null) return new ObservableCollection<Project>();

                // Ensure FolderPaths is not null
                foreach (var p in list)
                {
                    if (p.FolderPaths == null) p.FolderPaths = new ObservableCollection<string>();
                }

                return new ObservableCollection<Project>(list);
            }
            catch (JsonException jex)
            {
                try
                {
                    // Backup corrupted file for manual inspection
                    var path = GetFilePath();
                    var backup = Path.ChangeExtension(path, ".corrupted.backup.json");
                    File.Copy(path, backup, true);
                }
                catch { /* ignore backup failures */ }

                System.Windows.MessageBox.Show($"Workspace Hub could not read the saved projects file: the data appears to be corrupted. A backup has been created (if possible).\n{jex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return new ObservableCollection<Project>();
            }
            catch (UnauthorizedAccessException ua)
            {
                System.Windows.MessageBox.Show($"Workspace Hub could not access the data file due to insufficient permissions.\n{ua.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new ObservableCollection<Project>();
            }
            catch (IOException io)
            {
                System.Windows.MessageBox.Show($"Workspace Hub could not read the data file due to an IO error.\n{io.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new ObservableCollection<Project>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load projects:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new ObservableCollection<Project>();
            }
        }
    }
}
