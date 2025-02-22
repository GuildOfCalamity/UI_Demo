using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;

namespace UI_Demo;

public partial class RingShape : Path
{
    //=============================================================================================
    public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(
        nameof(Center),
        typeof(Point),
        typeof(RingShape), 
        new PropertyMetadata(null));
    public Point Center
    {
        get { return (Point)GetValue(CenterProperty); }
        set { SetValue(CenterProperty, value); }
    }

    //=============================================================================================
    public static readonly DependencyProperty SweepDirectionProperty = DependencyProperty.Register(
        nameof(SweepDirection),
        typeof(SweepDirection),
        typeof(RingShape),
        new PropertyMetadata(SweepDirection.Clockwise));
    public SweepDirection SweepDirection
    {
        get { return (SweepDirection)GetValue(SweepDirectionProperty); }
        set { SetValue(SweepDirectionProperty, value); }
    }

    //=============================================================================================
    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle),
        typeof(double),
        typeof(RingShape),
        new PropertyMetadata(0.0d, OnStartAngle));
    public double StartAngle
    {
        get { return (double)GetValue(StartAngleProperty); }
        set { SetValue(StartAngleProperty, value); }
    }
    static void OnStartAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnStartAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnStartAngleChanged(double oldValue, double newValue)
    {
        StartAngleChanged();
    }

    //=============================================================================================
    public static readonly DependencyProperty EndAngleProperty = DependencyProperty.Register(
        nameof(EndAngle),
        typeof(double),
        typeof(RingShape),
        new PropertyMetadata(90.0d, OnEndAngle));
    public double EndAngle
    {
        get { return (double)GetValue(EndAngleProperty); }
        set { SetValue(EndAngleProperty, value); }
    }
    static void OnEndAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnEndAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnEndAngleChanged(double oldValue, double newValue)
    {
        EndAngleChanged();
    }

    //=============================================================================================
    protected virtual void OnSweepDirectionChanged(SweepDirection oldValue, SweepDirection newValue)
    {
        SweepDirectionChanged();
    }

    //=============================================================================================
    public static readonly DependencyProperty MinAngleProperty = DependencyProperty.Register(
       nameof(MinAngle),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(0.0d, OnMinAngle));
    public double MinAngle
    {
        get { return (double)GetValue(MinAngleProperty); }
        set { SetValue(MinAngleProperty, value); }
    }
    static void OnMinAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnMinAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnMinAngleChanged(double oldValue, double newValue)
    {
        MinMaxAngleChanged(false);
    }

    //=============================================================================================
    public static readonly DependencyProperty MaxAngleProperty = DependencyProperty.Register(
       nameof(MaxAngle),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(360.0d, OnMaxAngle));
    public double MaxAngle
    {
        get { return (double)GetValue(MaxAngleProperty); }
        set { SetValue(MaxAngleProperty, value); }
    }
    static void OnMaxAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnMaxAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnMaxAngleChanged(double oldValue, double newValue)
    {
        MinMaxAngleChanged(true);
    }

    //=============================================================================================
    public static readonly DependencyProperty RadiusWidthProperty = DependencyProperty.Register(
       nameof(RadiusWidth),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(0.0d, OnRadiusWidth));
    public double RadiusWidth
    {
        get { return (double)GetValue(RadiusWidthProperty); }
        set { SetValue(RadiusWidthProperty, value); }
    }
    static void OnRadiusWidth(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnRadiusWidthChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnRadiusWidthChanged(double oldValue, double newValue)
    {
        RadiusWidthChanged();
    }

    //=============================================================================================
    public static readonly DependencyProperty RadiusHeightProperty = DependencyProperty.Register(
       nameof(RadiusHeight),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(0.0d, OnRadiusHeight));
    public double RadiusHeight
    {
        get { return (double)GetValue(RadiusHeightProperty); }
        set { SetValue(RadiusHeightProperty, value); }
    }
    static void OnRadiusHeight(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnRadiusHeightChanged((double)e.OldValue, (double)e.NewValue);
    }
    protected virtual void OnRadiusHeightChanged(double oldValue, double newValue)
    {
        RadiusHeightChanged();
    }

    //=============================================================================================
    public static readonly DependencyProperty IsCircleProperty = DependencyProperty.Register(
       nameof(IsCircle),
       typeof(bool),
       typeof(RingShape),
       new PropertyMetadata(false, OnIsCircle));
    public bool IsCircle
    {
        get { return (bool)GetValue(IsCircleProperty); }
        set { SetValue(IsCircleProperty, value); }
    }
    static void OnIsCircle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (RingShape)d;
        ctrl?.OnIsCircleChanged((bool)e.OldValue, (bool)e.NewValue);
    }
    protected virtual void OnIsCircleChanged(bool oldValue, bool newValue)
    {
        IsCircleChanged();
    }


    //=============================================================================================
    public static readonly DependencyProperty ActualRadiusWidthProperty = DependencyProperty.Register(
       nameof(ActualRadiusWidth),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(0.0d));
    public double ActualRadiusWidth
    {
        get { return (double)GetValue(ActualRadiusWidthProperty); }
        set { SetValue(ActualRadiusWidthProperty, value); }
    }

    //=============================================================================================
    public static readonly DependencyProperty ActualRadiusHeightProperty = DependencyProperty.Register(
       nameof(ActualRadiusHeight),
       typeof(double),
       typeof(RingShape),
       new PropertyMetadata(0.0d));
    public double ActualRadiusHeight
    {
        get { return (double)GetValue(ActualRadiusHeightProperty); }
        set { SetValue(ActualRadiusHeightProperty, value); }
    }


}
