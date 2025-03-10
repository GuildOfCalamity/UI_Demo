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
    Cache = 20,
    ProfileManager = 22,
    ToastHelper = 24,
    DialogHelper = 26,
    BlurHelper = 28,
    TaskbarHelper = 30,
    UIThreadHelper = 32,
    AppCapture = 34,
    MessageService = 36,
    Extensions = 38,
    Gibberish = 40,
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

public enum FileSystemItemType : byte
{
    Directory = 0,
    File = 1,
    Symlink = 2,
    Library = 3,
    Share = 4,
}
