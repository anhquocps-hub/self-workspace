using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using workspace_hub.Services;

namespace workspace_hub.Controls;

public partial class LinksEditor : System.Windows.Controls.UserControl
{
    public ObservableCollection<string> Links { get; } = new();

    public LinksEditor()
    {
        InitializeComponent();
        LinksList.ItemsSource = Links;
    }

    public bool CommitPending()
    {
        var url = UrlTextBox.Text.Trim();
        ErrorText.Visibility = Visibility.Collapsed;
        if (url.Length == 0) return true;
        if (!ProjectLinks.IsWebUrl(url))
        {
            ErrorText.Text = "URL không hợp lệ. Hãy nhập đầy đủ http:// hoặc https:// và tên miền.";
            ErrorText.Visibility = Visibility.Visible;
            UrlTextBox.Focus();
            return false;
        }
        if (!Links.Contains(url)) Links.Add(url);
        UrlTextBox.Clear();
        return true;
    }

    private void Add_Click(object sender, RoutedEventArgs e) => CommitPending();

    private void UrlTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        CommitPending();
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string url }) Links.Remove(url);
    }
}
