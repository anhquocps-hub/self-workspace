using System;
using System.Collections.Generic;
using System.Linq;
using workspace_hub.Models;

namespace workspace_hub.Services;

public static class ProjectLinks
{
    public static bool IsWebUrl(string? value) =>
        Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && !string.IsNullOrWhiteSpace(uri.Host)
        && !value!.Trim().Any(char.IsWhiteSpace);

    public static bool HasLocation(IEnumerable<string> folders, IEnumerable<string> links) =>
        folders.Any(folder => !string.IsNullOrWhiteSpace(folder)) || links.Any(IsWebUrl);

    public static string? FirstUrl(Project project) => project.MediaPaths.FirstOrDefault(IsWebUrl)?.Trim();

    public static void UpdateUrls(Project project, IEnumerable<string> links)
    {
        var preserved = project.MediaPaths.Where(path => !IsWebUrl(path)).ToList();
        var urls = links.Select(link => link.Trim()).Distinct(StringComparer.Ordinal).ToList();
        project.MediaPaths.Clear();
        foreach (var path in preserved) project.MediaPaths.Add(path);
        foreach (var url in urls) project.MediaPaths.Add(url);
    }
}
