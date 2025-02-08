using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace UI_Demo;

public sealed partial class SeparatorLine : UserControl
{
    bool _initialized = false;
    static Windows.UI.Color _color1 = Windows.UI.Color.FromArgb(200, 100, 120, 140);
    static Windows.UI.Color _color2 = Windows.UI.Color.FromArgb(200, 20, 30, 40);

    #region [Properties]
    public static readonly DependencyProperty Line1BrushProperty = DependencyProperty.Register(
        nameof(Line1Brush),
        typeof(Brush),
        typeof(SeparatorLine),
     new PropertyMetadata(new SolidColorBrush(_color1)));

    public Brush Line1Brush
    {
        get { return (Brush)GetValue(Line1BrushProperty); }
        set { SetValue(Line1BrushProperty, value); }
    }

    public static readonly DependencyProperty Line2BrushProperty = DependencyProperty.Register(
        nameof(Line2Brush),
        typeof(Brush),
        typeof(SeparatorLine),
     new PropertyMetadata(new SolidColorBrush(_color2)));

    public Brush Line2Brush
    {
        get { return (Brush)GetValue(Line2BrushProperty); }
        set { SetValue(Line2BrushProperty, value); }
    }

    /// <summary>
    /// The separator's height value.
    /// </summary>
    public double SeparatorThickness
    {
        get => (double)GetValue(SeparatorThicknessProperty);
        set => SetValue(SeparatorThicknessProperty, value);
    }
    /// <summary>
    /// Backing property for SeparatorHeight
    /// </summary>
    public static readonly DependencyProperty SeparatorThicknessProperty = DependencyProperty.Register(
        nameof(SeparatorThickness),
        typeof(double),
        typeof(SeparatorLine),
        new PropertyMetadata(2d, OnSeparatorThicknessPropertyChanged));
    /// <summary>
    /// Upon trigger, the <see cref="DependencyObject"/> will be the control itself (<see cref="SeparatorLine"/>)
    /// and the <see cref="DependencyPropertyChangedEventArgs"/> will be the <see cref="double"/> object contained within.
    /// </summary>
    static void OnSeparatorThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null && e.NewValue is double dbl)
        {
            // A "cheat" so we can use non-static local control variables.
            ((SeparatorLine)d).OnThicknessChanged((double)e.NewValue);
        }
        else if (e.NewValue != null)
        {
            Debug.WriteLine($"[WARNING] Wrong type => {e.NewValue?.GetType()}");
            Debugger.Break();
        }
    }
    /// <summary>
    ///   If this value gets too high then a CornerRadius adjustment may be required.
    /// </summary>
    void OnThicknessChanged(double newValue)
    {
        if (newValue > 0.01)
        {
            Line1Border.Height = newValue;
            Line2Border.Height = newValue;
        }
    }
    #endregion

    public SeparatorLine()
    {
        DataContext = this;
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        // This is just for effect, it serves no practical purpose.
        //this.PointerEntered += OnPointerEntered;
        //this.PointerExited += OnPointerExited;
    }

    #region [Events]
    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _initialized = true;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] Loaded {sender.GetType().Name} of base type {sender.GetType().BaseType?.Name}");
    }

    void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var clr1 = (Line1Brush as SolidColorBrush)?.Color ?? _color1;
        var clr2 = (Line2Brush as SolidColorBrush)?.Color ?? _color2;
        Line1Brush = new SolidColorBrush(clr2);
        Line2Brush = new SolidColorBrush(clr1);
    }

    void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var clr1 = (Line1Brush as SolidColorBrush)?.Color ?? _color2;
        var clr2 = (Line2Brush as SolidColorBrush)?.Color ?? _color1;
        Line1Brush = new SolidColorBrush(clr2);
        Line2Brush = new SolidColorBrush(clr1);
    }
    #endregion
}