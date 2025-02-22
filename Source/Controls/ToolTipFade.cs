using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace UI_Demo;

public sealed partial class ToolTipFade : ToolTip
{
    #region [Props]
    bool _initialized = false;
    Storyboard? _fadeInStoryboard;
    Storyboard? _fadeOutStoryboard;

    /// <summary>
    ///   The time it takes for the tooltip to fade in and out, in milliseconds.
    /// </summary>
    public static readonly DependencyProperty FadeTimeProperty = DependencyProperty.Register(
         nameof(FadeTime),
         typeof(double),
         typeof(ToolTipFade),
     new PropertyMetadata(300d, OnFadeTimeChanged));
    public double FadeTime
    {
        get => (double)GetValue(FadeTimeProperty);
        set => SetValue(FadeTimeProperty, value);
    }
    static void OnFadeTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ToolTipFade)d;
        if (ctrl != null)
            ctrl.ChangeFadeTime((double)e.NewValue);
    }
    void ChangeFadeTime(double value)
    {
        Debug.WriteLine($"[INFO] FadeTime is now {value} ms");
        InitializeAnimations();
    }
    #endregion

    public ToolTipFade()
    {
        this.DefaultStyleKey = typeof(ToolTip);

        // Initialize animations
        InitializeAnimations();
        
        // Hook into the IsOpenChanged event
        this.Opened += OnOpened;
        this.Closed += OnClosed;
    }

    /// <summary>
    ///   Entry method linked to styler.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        
        // Alter some of the base style properties.
        Opacity = 0.0;
        Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Mouse;
        Padding = new Thickness(0);
        BorderThickness = new Thickness(0);
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        _initialized = true;
    }

    void InitializeAnimations()
    {
        #region [Fade In]
        _fadeInStoryboard = new Storyboard();
        var fadeInAnimation = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(FadeTime),
            EnableDependentAnimation = true
        };
        // Set the fade-in animation target.
        Storyboard.SetTarget(fadeInAnimation, this);
        Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
        if (_fadeInStoryboard.Children.Remove(fadeInAnimation))
            Debug.WriteLine($"[INFO] Existing fadeInAnimation was removed.");
        _fadeInStoryboard.Children.Add(fadeInAnimation);
        #endregion

        #region [Fade Out]
        _fadeOutStoryboard = new Storyboard();
        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1, To = 0,
            // Cut hide time in half, since control's default behavior is close quickly.
            Duration = TimeSpan.FromMilliseconds(FadeTime / 2),
            EnableDependentAnimation = true
        };
        // Set the fade-out animation target.
        Storyboard.SetTarget(fadeOutAnimation, this);
        Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
        if (_fadeOutStoryboard.Children.Remove(fadeOutAnimation))
            Debug.WriteLine($"[INFO] Existing fadeOutAnimation was removed.");
        _fadeOutStoryboard.Children.Add(fadeOutAnimation);
        _fadeOutStoryboard.Completed -= FadeOutStoryboardOnCompleted;
        _fadeOutStoryboard.Completed += FadeOutStoryboardOnCompleted;
        #endregion
    }

    void FadeOutStoryboardOnCompleted(object? sender, object e)
    {
        Debug.WriteLine($"[INFO] FadeOutStoryboard was completed.");
        this.Visibility = Visibility.Collapsed;
    }

    void OnOpened(object sender, object e)
    {
        this.Visibility = Visibility.Visible;
        _fadeInStoryboard?.Begin();
    }
    /// <summary>
    ///   We don't have complete control over this since we are inheriting from the <see cref="ToolTip"/> base.
    ///   Long fade times will not be respected since the control's default nature is to close and fade quickly.
    /// </summary>
    void OnClosed(object sender, object e)
    {
        Debug.WriteLine($"[INFO] FadeOutStoryboard was started.");
        _fadeOutStoryboard?.Begin();
    }
}