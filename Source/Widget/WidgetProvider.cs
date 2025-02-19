using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;

namespace UI_Demo;

[ComVisible(true)]
[ComDefaultInterface(typeof(IWidgetProvider))]
[Guid("80DAE84C-DE3C-4F02-8ECD-DCDD1CDECC15")]
public sealed class WidgetProvider : IWidgetProvider
{
    public WidgetProvider()
    {
        RecoverRunningWidgets();
    }

    private static bool HaveRecoveredWidgets { get; set; } = false;
    private static void RecoverRunningWidgets()
    {
        if (!HaveRecoveredWidgets)
        {
            try
            {
                var widgetManager = WidgetManager.GetDefault();
                foreach (var widgetInfo in widgetManager.GetWidgetInfos())
                {
                    var context = widgetInfo.WidgetContext;
                    if (!WidgetInstances.ContainsKey(context.Id))
                    {
                        if (WidgetImpls.ContainsKey(context.DefinitionId))
                        {
                            // Need to recover this instance
                            WidgetInstances[context.Id] = WidgetImpls[context.DefinitionId](context.Id, widgetInfo.CustomState);
                        }
                        else
                        {
                            // this provider doesn't know about this type of Widget (any more?) delete it
                            widgetManager.DeleteWidget(context.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] RecoverRunningWidgets: {ex.Message}");
            }
            finally
            {
                HaveRecoveredWidgets = true;
            }
        }
    }

    static readonly Dictionary<string, WidgetCreateDelegate> WidgetImpls = new() 
    {
        [CountingWidget.DefinitionId] = (widgetId, initialState) => new CountingWidget(widgetId, initialState),
        [WeatherWidget.DefinitionId] = (widgetId, initialState) => new WeatherWidget(widgetId, initialState),
    };

    private static Dictionary<string, WidgetImplBase> WidgetInstances = new();

    // Handle the CreateWidget call. During this function call you should store
    // the WidgetId value so you can use it to update corresponding widget.
    // It is our way of notifying you that the user has pinned your widget
    // and you should start pushing updates.
    public void CreateWidget(WidgetContext widgetContext)
    {
        if (widgetContext is null)
        {
            Debug.WriteLine($"[WARNING] '{nameof(widgetContext)}' cannot be null");
            return;
        }

        Debug.WriteLine($"CreateWidget id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");

        if (!WidgetImpls.ContainsKey(widgetContext.DefinitionId))
        {
            Debug.WriteLine($"ERROR: Requested unknown Widget Definition ${widgetContext.DefinitionId}");
            throw new Exception($"Invalid definition requested: {widgetContext.DefinitionId}");
        }

        var widgetInstance = WidgetImpls[widgetContext.DefinitionId](widgetContext.Id, "");
        WidgetInstances[widgetContext.Id] = widgetInstance;

        WidgetUpdateRequestOptions options = new WidgetUpdateRequestOptions(widgetContext.Id);
        options.Template = widgetInstance.GetTemplateForWidget();
        options.Data = widgetInstance.GetDataForWidget();
        options.CustomState = widgetInstance.State;

        Debug.WriteLine("Sending payload:");
        Debug.WriteLine($"---Template---\n{options.Template}\n---\n");
        Debug.WriteLine($"---Data---\n{options.Data}\n---\n");
        Debug.WriteLine($"---Custom State---\n{options.CustomState}\n---\n");

        WidgetManager.GetDefault().UpdateWidget(options);
    }

    // Handle the DeleteWidget call. This is notifying you that
    // you don't need to provide new content for the given WidgetId
    // since the user has unpinned the widget or it was deleted by the Host
    // for any other reason.
    public void DeleteWidget(string widgetId, string _)
    {
        Debug.WriteLine($"DeleteWidget id: {widgetId}");

        WidgetInstances.Remove(widgetId);

        var widgetIds = WidgetManager.GetDefault().GetWidgetIds();
        if (widgetIds != null)
        {
            Debug.WriteLine($"  {widgetIds.Length.ToString()} Remaining Widgets:");
            foreach (var remainingWidgetId in widgetIds)
            {
                Debug.WriteLine($"    {remainingWidgetId}");
            }
        }
        else if (widgetIds == null || widgetIds.Length == 0)
        {
            Debug.WriteLine("  (no more Widgets outstanding)");
        }
    }

    // Handle the OnActionInvoked call. This function call is fired when the user's
    // interaction with the widget resulted in an execution of one of the defined
    // actions that you've indicated in the template of the Widget.
    // For example: clicking a button or submitting input.
    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        Debug.WriteLine($"OnActionInvoked id: {actionInvokedArgs.WidgetContext.Id} definitionId: {actionInvokedArgs.WidgetContext.DefinitionId}");

        WidgetInstances[actionInvokedArgs.WidgetContext.Id].OnActionInvoked(actionInvokedArgs);
    }

    // Handle the WidgetContextChanged call. This function is called when the context a widget
    // has changed. Currently it only signals that the user has changed the size of the widget.
    // There are 2 ways to respond to this event:
    // 1) Call UpdateWidget() with the new data/template to fit the new requested size.
    // 2) If previously sent data/template accounts for various sizes based on $host.widgetSize - you can use this event solely for telemtry.
    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Debug.WriteLine($"OnWidgetContextChanged id: {contextChangedArgs.WidgetContext.Id} definitionId: {contextChangedArgs.WidgetContext.DefinitionId}");
        WidgetInstances[contextChangedArgs.WidgetContext.Id].OnWidgetContextChanged(contextChangedArgs);
    }

    // Handle the Activate call. This function is called when widgets host starts listening
    // to the widget updates. It generally happens when the widget becomes visible and the updates
    // will be promptly displayed to the user. It's recommended to start sending updates from this moment
    // until Deactivate function was called.
    public void Activate(WidgetContext widgetContext)
    {
        Debug.WriteLine($"Activate id: {widgetContext.Id} definitionId: {widgetContext.DefinitionId}");

        if (!WidgetInstances.ContainsKey(widgetContext.Id))
        {
            throw new Exception($"Activate called for unknown ");
        }
    }

    // Handle the Deactivate call. This function is called when widgets host stops listening
    // to the widget updates. It generally happens when the widget is not visible to the user
    // anymore and any further updates won't be displayed until the widget is visible again.
    // It's recommended to stop sending updates until Activate function was called.
    public void Deactivate(string widgetId)
    {
        Debug.WriteLine($"Deactivate id: {widgetId}");
        WidgetInstances[widgetId].Deactivate();
    }
}
