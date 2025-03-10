using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace UI_Demo;

public class FileWatcher
{
    readonly List<FileSystemWatcher> _watchers = [];

    /// <summary>
    /// Gets invoked when an item addition is detected by the watcher
    /// </summary>
    public event EventHandler<FileSystemEventArgs>? ItemAdded;

    /// <summary>
    /// Gets invoked when an item removal is detected by the watcher
    /// </summary>
    public event EventHandler<FileSystemEventArgs>? ItemDeleted;

    /// <summary>
    /// Gets invoked when an item changing is detected by the watcher
    /// </summary>
    public event EventHandler<FileSystemEventArgs>? ItemChanged;

    /// <summary>
    /// Gets invoked when an item renaming is detected by the watcher
    /// </summary>
    public event EventHandler<FileSystemEventArgs>? ItemRenamed;

    /// <summary>
    /// Gets invoked when an refresh request is detected by the watcher
    /// </summary>
    public event EventHandler<FileSystemEventArgs>? RefreshRequested;

    public FileWatcher(List<string> paths, bool subfolders = false)
    {
        StartWatcher(paths, subfolders);
    }

    public void StartWatcher(List<string> paths, bool subfolders)
    {
        // Listen to changes only on the path the current logged-on user has.
        var sid = WindowsIdentity.GetCurrent().User?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(sid))
            return;

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
                continue;

            // Suppress any NullReferenceException caused by EnableRaisingEvents
            Extensions.IgnoreExceptions(() =>
            {
                FileSystemWatcher watcher = new()
                {
                    Path = path,
                    Filter = "*.*",
                    IncludeSubdirectories = subfolders,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                };

                watcher.Created += WatcherOnModify;
                watcher.Deleted += WatcherOnModify;
                watcher.Changed += WatcherOnModify;
                //watcher.Renamed += WatcherOnChanged;
                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            });
        }
    }

    void WatcherOnModify(object sender, FileSystemEventArgs e)
    {
        // Ignore system files starting with '$I'
        if (string.IsNullOrEmpty(e.Name) || e.Name.StartsWith("$I", StringComparison.Ordinal))
            return;

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                ItemAdded?.Invoke(this, e);
                break;
            case WatcherChangeTypes.Deleted:
                ItemDeleted?.Invoke(this, e);
                break;
            case WatcherChangeTypes.Renamed:
                ItemRenamed?.Invoke(this, e);
                break;
            case WatcherChangeTypes.Changed:
                ItemChanged?.Invoke(this, e);
                break;
            default:
                RefreshRequested?.Invoke(this, e);
                break;
        }
    }

    public void StopWatcher()
    {
        foreach (var watcher in _watchers)
            watcher.Dispose();
    }

    public void Dispose()
    {
        StopWatcher();
    }
}

