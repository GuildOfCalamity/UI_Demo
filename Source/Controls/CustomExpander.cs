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
    DispatcherTimer? _tmrCollapse;
    bool _initialized = false;
    long _cpToken { get; set; }

    #region [Dependency Properties]
    public double ExpanderHeight
    {
        get => (double)GetValue(ExpanderHeightProperty);
        set => SetValue(ExpanderHeightProperty, value);
    }
    public static readonly DependencyProperty ExpanderHeightProperty = DependencyProperty.Register(
        nameof(ExpanderHeight),
        typeof(double),
        typeof(CustomExpander),
        new PropertyMetadata(36, OnExpanderHeightChanged));

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

    public double AutoCollapseTime
    {
        get => (double)GetValue(AutoCollapseTimeProperty);
        set => SetValue(AutoCollapseTimeProperty, value);
    }
    public static readonly DependencyProperty AutoCollapseTimeProperty = DependencyProperty.Register(
        nameof(AutoCollapseTime),
        typeof(double),
        typeof(CustomExpander),
        new PropertyMetadata(8d, OnCollapseTimeChanged));
    static void OnCollapseTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomExpander)d;
        if (e.NewValue is double newValue)
            control.SetCollapseTime(newValue);
    }
    void SetCollapseTime(double newValue)
    {
         if (_tmrCollapse is not null)
             _tmrCollapse.Interval = TimeSpan.FromSeconds(AutoCollapseTime);
    }
 
    public bool AutoCollapse
    {
        get => (bool)GetValue(AutoCollapseProperty);
        set => SetValue(AutoCollapseProperty, value);
    }
    public static readonly DependencyProperty AutoCollapseProperty = DependencyProperty.Register(
        nameof(AutoCollapse),
        typeof(bool),
        typeof(CustomExpander),
        new PropertyMetadata(false, OnAutoCollapseChanged));

    static void OnAutoCollapseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CustomExpander)d;
        if (e.NewValue is bool newValue)
            control.SetAutoCollapse(newValue);
    }
    void SetAutoCollapse(bool enabled)
    {
        if (enabled)
        {
            if (_tmrCollapse == null)
            {
                _tmrCollapse = new DispatcherTimer();
                _tmrCollapse.Interval = TimeSpan.FromSeconds(AutoCollapseTime);
                _tmrCollapse.Tick += CollapseTimerOnTick;
                //_tmrCollapse.Start();
            }
            else
                _tmrCollapse?.Stop();
        }
        else
        {
            if (_tmrCollapse != null)
            {
                _tmrCollapse.Tick -= CollapseTimerOnTick;
                _tmrCollapse?.Stop();
                _tmrCollapse = null;
            }
        }
    }
    #endregion

    public CustomExpander()
    {
        this.Loaded += OnCustomExpanderLoaded;
        this.Unloaded += OnCustomExpanderUnloaded;
        this.PointerExited += OnPointerExited;
        this.PointerEntered += OnPointerEntered;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        _initialized = true;
    }

    void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] Expander => PointerEntered {DateTime.Now.ToLongTimeString()}");
        if (AutoCollapse)
            _tmrCollapse?.Stop();
    }

    void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] Expander => PointerExited {DateTime.Now.ToLongTimeString()}");
        if (AutoCollapse)
            _tmrCollapse?.Start();
    }

    void CollapseTimerOnTick(object? sender, object e)
    {
        if (this.IsExpanded)
        {
            Debug.WriteLine($"[INFO] Expander => TimerTick: COLLAPSE");
            this.IsExpanded = false;
            _tmrCollapse?.Stop();
        }
        else
        {
            Debug.WriteLine($"[INFO] Expander => TimerTick: IGNORE");
        }
    }

    void OnCustomExpanderLoaded(object sender, RoutedEventArgs e)
    {
        _cpToken = RegisterPropertyChangedCallback(Expander.ContentProperty, OnContentPropertyChanged);
    }

    void OnContentPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        this.IsExpanded = true;
    }

    void OnCustomExpanderUnloaded(object sender, RoutedEventArgs e)
    {
        UnregisterPropertyChangedCallback(Expander.ContentProperty, _cpToken);
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
