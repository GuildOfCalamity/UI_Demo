using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_Demo;

/// <summary>
///   Define our event args class for the <see cref="EventBus"/>.
///   This example uses an object value that could be switched upon in the main UI update 
///   routine, but more complex object types could be passed to encapsulate additional information.
/// </summary>
public class ObjectEventArgs : EventArgs
{
    public object? Payload { get; }
    public ObjectEventArgs(object? payload)
    {
        Payload = payload;
    }
}
