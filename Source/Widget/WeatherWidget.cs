using System;
using System.Text.Json.Nodes;
using Microsoft.Windows.Widgets.Providers;

namespace UI_Demo;

internal class WeatherWidget : WidgetImplBase
{
    public static string DefinitionId { get; } = "Weather_Widget";
    public WeatherWidget(string widgetId, string initialState) : base(widgetId, initialState) { }

    private static string WidgetTemplate { get; set; } = "";

    private static string GetDefaultTemplate()
    {
        if (string.IsNullOrEmpty(WidgetTemplate))
        {
            WidgetTemplate = ReadPackageFileFromUri("ms-appx:///Widget/Templates/WeatherWidgetTemplate.json");
        }

        return WidgetTemplate;
    }

    public override string GetTemplateForWidget()
    {
        return GetDefaultTemplate();
    }

    public override string GetDataForWidget()
    {
        // Return empty JSON since we don't have any data that we want to use.
        return "{}";
    }
}
