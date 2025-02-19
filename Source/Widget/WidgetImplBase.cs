using System;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets.Providers;
using Windows.Storage;

namespace UI_Demo;

internal delegate WidgetImplBase WidgetCreateDelegate(string widgetId, string initialState);
internal abstract class WidgetImplBase
{
    protected string id;
    protected string state;
    protected bool isActivated = false;

    protected WidgetImplBase(string widgetId, string initialState)
    {
        state = initialState;
        id = widgetId;
    }

    public string Id { get => id; }
    public string State { get => state; }
    public bool IsActivated { get => isActivated; }

    /// <summary>
    /// Returns the contents of a packaged file from the application, e.g.
    /// <code>
    ///   var content = ReadPackageFileFromUri("ms-appx:///Widget/Templates/CountingWidgetTemplate.json");
    /// </code>
    /// </summary>
    protected static string ReadPackageFileFromUri(string packageUri)
    {
        var uri = new Uri(packageUri);
        var storageFileTask = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask();
        storageFileTask.Wait();
        var readTextTask = FileIO.ReadTextAsync(storageFileTask.Result).AsTask();
        readTextTask.Wait();
        return readTextTask.Result;
    }

    /// <summary>
    /// Returns the contents of a packaged file from the application, e.g.
    /// <code>
    ///   var content = ReadPackageFileFromUri("ms-appx:///Widget/Templates/CountingWidgetTemplate.json");
    /// </code>
    /// </summary>
    protected static async Task<string> ReadPackageFileFromUriAsync(string packageUri)
    {
        var uri = new Uri(packageUri);
        var storageFileTask = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
        var readTextTask = await Windows.Storage.FileIO.ReadTextAsync(storageFileTask);
        return readTextTask;
    }

    public virtual void Activate(WidgetContext widgetContext) { }
    public virtual void Deactivate() { }
    public virtual void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs) { }
    public virtual void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs) { }

    public abstract string GetTemplateForWidget();
    public abstract string GetDataForWidget();
}