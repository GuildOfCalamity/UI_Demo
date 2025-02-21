using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Demo;

public enum MessageLevel
{
    Debug = 0,
    Information = 1,
    Important = 2,
    Warning = 3,
    Error = 4
}

public enum ModuleId
{
    None = 0,
    App = 1,
    MainWindow = 2,
    MainPage = 3,
    ControlsPage = 4,
    ProfileManager = 21,
    ToastHelper = 22,
    DialogHelper = 23,
    BlurHelper = 24,
    TaskbarHelper = 25,
    UIThreadHelper = 26,
    AppCapture = 27,
    MessageService = 28,
    Extensions = 29,
    Gibberish = 30,
}

public enum ChannelMessageType
{
    AppHeartbeat = 0,
    AppSize = 1,
    AppState = 2,
    AppClose = 3,
    AppException = 4
}

public enum DelayTime
{
    None = 0,
    Short = 1,
    Medium = 3,
    Long = 5
}

/// <summary>
/// Defines constants that specify item type of the file system on Windows.
/// </summary>
public enum FileSystemItemType : byte
{
    /// <summary>
    /// The item is a directory.
    /// </summary>
    Directory = 0,

    /// <summary>
    /// The item is a file.
    /// </summary>
    File = 1,

    /// <summary>
    /// The item is a symlink.
    /// </summary>
    [Obsolete("The symlink has no use for now here.")]
    Symlink = 2,

    /// <summary>
    /// The item is a library.
    /// </summary>
    Library = 3,
}
