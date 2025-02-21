using Microsoft.UI.Xaml;

namespace UI_Demo;

public partial class StorageBar
{
    //=============================================================================================
    public static readonly DependencyProperty ValueBarHeightProperty = DependencyProperty.Register(
        nameof(ValueBarHeight),
        typeof(double),
        typeof(StorageBar),
        new PropertyMetadata(4.0d, OnValueBarHeight));
    public double ValueBarHeight
    {
        get { return (double)GetValue(ValueBarHeightProperty); }
        set { SetValue(ValueBarHeightProperty, value); }
    }
    static void OnValueBarHeight(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnValueBarHeightChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnValueBarHeightChanged(double oldValue, double newValue) => UpdateControl(this);

    //=============================================================================================
    public static readonly DependencyProperty TrackBarHeightProperty = DependencyProperty.Register(
        nameof(TrackBarHeight),
        typeof(double),
        typeof(StorageBar),
        new PropertyMetadata(2.0d, OnTrackBarHeight));
    public double TrackBarHeight
    {
        get { return (double)GetValue(TrackBarHeightProperty); }
        set { SetValue(TrackBarHeightProperty, value); }
    }
    static void OnTrackBarHeight(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnTrackBarHeightChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnTrackBarHeightChanged(double oldValue, double newValue) => UpdateControl(this);

    //=============================================================================================
    public static readonly DependencyProperty BarShapeProperty = DependencyProperty.Register(
        nameof(BarShape),
        typeof(BarShapes),
        typeof(StorageBar),
        new PropertyMetadata(BarShapes.Round, OnBarShape));
    public BarShapes BarShape
    {
        get { return (BarShapes)GetValue(BarShapeProperty); }
        set { SetValue(BarShapeProperty, value); }
    }
    static void OnBarShape(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnBarShapeChanged((BarShapes)e.OldValue, (BarShapes)e.NewValue);
    }
    void OnBarShapeChanged(BarShapes oldValue, BarShapes newValue) => UpdateControl(this);

    //=============================================================================================
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent),
        typeof(double),
        typeof(StorageBar),
        new PropertyMetadata(0.0d, OnPercent));
    public double Percent
    {
        get { return (double)GetValue(PercentProperty); }
        set { SetValue(PercentProperty, value); }
    }
    static void OnPercent(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnPercentChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentChanged(double oldValue, double newValue)
    {
        return; //Read-only
        DoubleToPercentage(Value, Minimum, Maximum);
        UpdateControl(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty PercentCautionProperty = DependencyProperty.Register(
        nameof(PercentCaution),
        typeof(double),
        typeof(StorageBar),
        new PropertyMetadata(75.1d, OnPercentCaution));
    public double PercentCaution
    {
        get { return (double)GetValue(PercentCautionProperty); }
        set { SetValue(PercentCautionProperty, value); }
    }
    static void OnPercentCaution(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnPercentCautionChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentCautionChanged(double oldValue, double newValue) => UpdateControl(this);

    //=============================================================================================
    public static readonly DependencyProperty PercentCriticalProperty = DependencyProperty.Register(
        nameof(PercentCritical),
        typeof(double),
        typeof(StorageBar),
        new PropertyMetadata(89.9d, OnPercentCritical));
    public double PercentCritical
    {
        get { return (double)GetValue(PercentCriticalProperty); }
        set { SetValue(PercentCriticalProperty, value); }
    }
    static void OnPercentCritical(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageBar)d;
        ctrl?.OnPercentCriticalChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentCriticalChanged(double oldValue, double newValue) => UpdateControl(this);

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        _oldValue = oldValue;
        base.OnValueChanged(oldValue, newValue);
        UpdateValue(this, Value, _oldValue, false, -1.0);
    }

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnMaximumChanged(double oldValue, double newValue)
    {
        base.OnMaximumChanged(oldValue, newValue);
        UpdateValue(this, oldValue, newValue, false, -1.0);
    }

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnMinimumChanged(double oldValue, double newValue)
    {
        base.OnMinimumChanged(oldValue, newValue);
        UpdateValue(this, oldValue, newValue, false, -1.0);
    }
}
