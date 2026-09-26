using System.Windows;
using System.Windows.Input;

namespace workspace_hub.Controls;

public partial class ProjectCard : ProjectControl
{
    public ProjectCard() => InitializeComponent();
    private void OpenButton_Click(object sender, RoutedEventArgs e) => RequestOpen();
    private void DetailsButton_Click(object sender, RoutedEventArgs e) => RequestDetails();
    private void Summary_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        RequestDetails();
    }
}