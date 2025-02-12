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
