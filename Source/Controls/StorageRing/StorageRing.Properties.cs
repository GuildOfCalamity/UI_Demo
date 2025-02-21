using Microsoft.UI.Xaml;

namespace UI_Demo;

public partial class StorageRing
{
    //=============================================================================================
    public static readonly DependencyProperty ValueAngleProperty = DependencyProperty.Register(
        nameof(ValueAngle),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d));
    public double ValueAngle
    {
        get { return (double)GetValue(ValueAngleProperty); }
        set { SetValue(ValueAngleProperty, value); }
    }

    //=============================================================================================
    public static readonly DependencyProperty AdjustedSizeProperty = DependencyProperty.Register(
        nameof(AdjustedSize),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(16.0d));
    public double AdjustedSize
    {
        get { return (double)GetValue(AdjustedSizeProperty); }
        set { SetValue(AdjustedSizeProperty, value); }
    }

    //=============================================================================================
    public static readonly DependencyProperty ValueRingThicknessProperty = DependencyProperty.Register(
        nameof(ValueRingThickness),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnValueRing));
    public double ValueRingThickness
    {
        get { return (double)GetValue(ValueRingThicknessProperty); }
        set { SetValue(ValueRingThicknessProperty, value); }
    }
    static void OnValueRing(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnValueRingThicknessChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnValueRingThicknessChanged(double oldValue, double newValue)
    {
        UpdateRingThickness(this, newValue, false);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty TrackRingThicknessProperty = DependencyProperty.Register(
        nameof(TrackRingThickness),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnTrackRing));
    public double TrackRingThickness
    {
        get { return (double)GetValue(TrackRingThicknessProperty); }
        set { SetValue(TrackRingThicknessProperty, value); }
    }
    static void OnTrackRing(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnTrackRingThicknessChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnTrackRingThicknessChanged(double oldValue, double newValue)
    {
        UpdateRingThickness(this, newValue, true);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty MinAngleProperty = DependencyProperty.Register(
        nameof(MinAngle),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnMinAngle));
    public double MinAngle
    {
        get { return (double)GetValue(MinAngleProperty); }
        set { SetValue(MinAngleProperty, value); }
    }
    static void OnMinAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnMinAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnMinAngleChanged(double oldValue, double newValue)
    {
        UpdateValues(this, Value, _oldValue, false, -1.0);
        CalculateAndSetNormalizedAngles(this, newValue, MaxAngle);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty MaxAngleProperty = DependencyProperty.Register(
        nameof(MaxAngle),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnMaxAngle));
    public double MaxAngle
    {
        get { return (double)GetValue(MaxAngleProperty); }
        set { SetValue(MaxAngleProperty, value); }
    }
    static void OnMaxAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnMaxAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnMaxAngleChanged(double oldValue, double newValue)
    {
        UpdateValues(this, Value, _oldValue, false, -1.0);
        CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
        nameof(StartAngle),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnStartAngle));
    public double StartAngle
    {
        get { return (double)GetValue(StartAngleProperty); }
        set { SetValue(StartAngleProperty, value); }
    }
    static void OnStartAngle(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnStartAngleChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnStartAngleChanged(double oldValue, double newValue)
    {
        UpdateValues(this, Value, _oldValue, false, -1.0);
        CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
        ValidateStartAngle(this, newValue);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnPercent));
    public double Percent
    {
        get { return (double)GetValue(PercentProperty); }
        set { SetValue(PercentProperty, value); }
    }
    static void OnPercent(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnPercentChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentChanged(double oldValue, double newValue)
    {
        return; //Read-only

        DoubleToPercentage(Value, Minimum, Maximum);

        double adjustedPercentage;

        if (newValue <= 0.0)
            adjustedPercentage = 0.0;
        else if (newValue <= 100.0)
            adjustedPercentage = 100.0;
        else
            adjustedPercentage = newValue;

        UpdateValues(this, Value, _oldValue, true, adjustedPercentage);
        UpdateVisualState(this);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty PercentCautionProperty = DependencyProperty.Register(
        nameof(PercentCaution),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnPercentCaution));
    public double PercentCaution
    {
        get { return (double)GetValue(PercentCautionProperty); }
        set { SetValue(PercentCautionProperty, value); }
    }
    static void OnPercentCaution(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnPercentCautionChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentCautionChanged(double oldValue, double newValue)
    {
        UpdateValues(this, Value, _oldValue, false, -1.0);
        UpdateVisualState(this);
        UpdateRings(this);
    }

    //=============================================================================================
    public static readonly DependencyProperty PercentCriticalProperty = DependencyProperty.Register(
        nameof(PercentCritical),
        typeof(double),
        typeof(StorageRing),
        new PropertyMetadata(0.0d, OnPercentCritical));
    public double PercentCritical
    {
        get { return (double)GetValue(PercentCriticalProperty); }
        set { SetValue(PercentCriticalProperty, value); }
    }
    static void OnPercentCritical(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (StorageRing)d;
        ctrl?.OnPercentCriticalChanged((double)e.OldValue, (double)e.NewValue);
    }
    void OnPercentCriticalChanged(double oldValue, double newValue)
    {
        UpdateValues(this, Value, _oldValue, false, -1.0);
        UpdateVisualState(this);
        UpdateRings(this);
    }

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);
        StorageRing_ValueChanged(this, newValue, oldValue);
    }

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        StorageRing_MinimumChanged(this, newMinimum);
    }

    //=============================================================================================
    /// <inheritdoc/>
    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        StorageRing_MaximumChanged(this, newMaximum);
    }
}
