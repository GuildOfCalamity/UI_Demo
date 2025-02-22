using System;
using System.Threading.Tasks;

using Windows.Foundation;

namespace UI_Demo;

// Wrapper class to implement IAsyncAction
public class AsyncActionWrapper : IAsyncAction
{
    readonly Task _task;
    AsyncActionCompletedHandler IAsyncAction.Completed { get; set; }
    public Exception ErrorCode { get; }
    public uint Id { get; }
    public AsyncStatus Status { get; }

    public AsyncActionWrapper(Task task)
    {
        _task = task;
    }

    public void Cancel()
    {
        // You can add cancellation logic here if needed
        // For example, you could call _task.Cancel() if applicable
    }

    public void GetResults()
    {
        // This method is not used for IAsyncAction
    }

    public void Close()
    {
        // This method is not used for IAsyncAction
    }

    public void Dispose()
    {
        // Dispose of resources if necessary
    }
}
