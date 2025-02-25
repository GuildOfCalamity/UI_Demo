using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

namespace UI_Demo;

/// <summary>
/// Automatically opens when the content changes.
/// </summary>
public partial class CustomExpander : Expander
{
    bool _initialized = false;
    long ContentPropertyToken { get; set; }

    public double ExpanderHeight
    {
        get => (double)GetValue(ExpanderHeightProperty);
        set => SetValue(ExpanderHeightProperty, value);
    }
    public static readonly DependencyProperty ExpanderHeightProperty = DependencyProperty.Register(
        nameof(ExpanderHeight),
        typeof(double),
        typeof(CustomExpander),
        new PropertyMetadata(32, OnExpanderHeightChanged));

    static void OnExpanderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomExpander)d;
        if (e.NewValue is double newValue)
            control.SetContentHeight(newValue);
    }

    public double ExpanderWidth
    {
        get => (double)GetValue(ExpanderWidthProperty);
        set => SetValue(ExpanderWidthProperty, value);
    }
    public static readonly DependencyProperty ExpanderWidthProperty = DependencyProperty.Register(
        nameof(ExpanderWidth),
        typeof(double),
        typeof(CustomExpander),
        new PropertyMetadata(100, OnExpanderWidthChanged));

    static void OnExpanderWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomExpander)d;
        if (e.NewValue is double newValue)
            control.SetContentWidth(newValue);
    }

    public CustomExpander()
    {
        this.Loaded += OnCustomExpanderLoaded;
        this.Unloaded += OnCustomExpanderUnloaded;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        _initialized = true;
    }


    void OnCustomExpanderLoaded(object sender, RoutedEventArgs e)
    {
        ContentPropertyToken = RegisterPropertyChangedCallback(Expander.ContentProperty, OnContentPropertyChanged);
    }

    void OnContentPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        this.IsExpanded = true;
    }

    void OnCustomExpanderUnloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPropertyChangedCallback(Expander.ContentProperty, ContentPropertyToken);
    }
}

public static class ExpanderExtensions
{
    /// <summary>
    /// Enables or disables the Header.
    /// </summary>
    public static void IsLocked(this Expander expander, bool locked)
    {
        var ctrl = (expander.Header as FrameworkElement)?.Parent as Control;
        if (ctrl != null)
            ctrl.IsEnabled = locked;
    }

    /// <summary>
    /// Sets desired Height for content when expanded.
    /// </summary>
    public static void SetContentHeight(this Expander expander, double contentHeight)
    {
        var ctrl = expander.Content as FrameworkElement;
        if (ctrl != null)
            ctrl.Height = contentHeight;
    }

    /// <summary>
    /// Sets desired Width for content when expanded.
    /// </summary>
    public static void SetContentWidth(this Expander expander, double contentWidth)
    {
        var ctrl = expander.Content as FrameworkElement;
        if (ctrl != null)
            ctrl.Width = contentWidth;
    }
}
