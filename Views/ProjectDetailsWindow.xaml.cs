using System.Windows;
using workspace_hub.Models;

namespace workspace_hub.Views;

public partial class ProjectDetailsWindow : Window
{
    public ProjectDetailsWindow(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        InitializeComponent();
        DetailsContent.Project = project;
        DetailsContent.CloseRequested += (_, _) => Close();
    }
}