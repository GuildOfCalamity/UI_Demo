using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace UI_Demo;

public class ButtonEx : Button
{
    bool _initialized = false;
    bool _addBlendedGradientStop = false;
    double _timeScale = 0.120d; // in seconds
    Vector2 _anchorPoint = Vector2.Zero;

    #region [Props]
    /// <summary>
    /// Identifies the <see cref="From"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty ColorFromProperty = DependencyProperty.Register(
        nameof(ColorFrom),
        typeof(Windows.UI.Color),
        typeof(ButtonEx),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the from color.
    /// </summary>
    public Windows.UI.Color ColorFrom
    {
        get => (Windows.UI.Color)GetValue(ColorFromProperty);
        set => SetValue(ColorFromProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="To"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty ColorToProperty = DependencyProperty.Register(
        nameof(ColorTo),
        typeof(Windows.UI.Color),
        typeof(ButtonEx),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the to color.
    /// </summary>
    public Windows.UI.Color ColorTo
    {
        get => (Windows.UI.Color)GetValue(ColorToProperty);
        set => SetValue(ColorToProperty, value);
    }

    public bool EnableScaleAnimation
    {
        get => (bool)GetValue(EnableScaleAnimationProperty);
        set => SetValue(EnableScaleAnimationProperty, value);
    }
    public static readonly DependencyProperty EnableScaleAnimationProperty = DependencyProperty.Register(
        nameof(EnableScaleAnimation),
        typeof(bool),
        typeof(ButtonEx),
        new PropertyMetadata(false));

    public double ScaleAmount
    {
        get => (double)GetValue(ScaleAmountProperty);
        set => SetValue(ScaleAmountProperty, value);
    }
    public static readonly DependencyProperty ScaleAmountProperty = DependencyProperty.Register(
        nameof(ScaleAmount),
        typeof(double),
        typeof(ButtonEx),
        new PropertyMetadata(1.1d));

    public double ScaleTime
    {
        get => (double)GetValue(ScaleTimeProperty);
        set => SetValue(ScaleTimeProperty, value);
    }
    public static readonly DependencyProperty ScaleTimeProperty = DependencyProperty.Register(
        nameof(ScaleTime),
        typeof(double),
        typeof(ButtonEx),
        new PropertyMetadata(0.12d));
    #endregion

    public ButtonEx()
    {
        this.DefaultStyleKey = typeof(Button);
        this.Loaded += ButtonExOnLoaded;
        this.Unloaded += ButtonExOnUnloaded;
        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        _initialized = true;
    }

    #region [Events]
    void ButtonExOnLoaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        AnimateUIElementColor(ColorFrom, ColorTo, (UIElement)sender);
        _anchorPoint = ElementCompositionPreview.GetElementVisual((UIElement)sender).AnchorPoint;
    }

    void ButtonExOnUnloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} unloaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        AnimateUIElementColor(ColorTo, ColorFrom, (UIElement)sender);
        if (EnableScaleAnimation)
            AnimateUIElementScale(1.0d, ScaleAmount, TimeSpan.FromSeconds(ScaleTime), (UIElement)sender, "QuarticEaseOut");
    }

    void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        this.ProtectedCursor = null;
        AnimateUIElementColor(ColorFrom, ColorTo, (UIElement)sender);
        if (EnableScaleAnimation)
            AnimateUIElementScale(ScaleAmount, 1.0d, TimeSpan.FromSeconds(ScaleTime), (UIElement)sender, "QuarticEaseIn");
    }
    #endregion

    #region [Composition Animations]
    /// <summary>
    /// This provides a gradient brush effect as an overlay. It is not an animation,
    /// only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    /// https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    void AnimateUIElementColor(Windows.UI.Color from, Windows.UI.Color to, UIElement element)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(element);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var spriteVisual = compositor.CreateSpriteVisual();
        if (spriteVisual is null) { return; }

        var gb = compositor.CreateLinearGradientBrush();
        // Define gradient stops.
        var gradientStops = gb.ColorStops;
        if (!_addBlendedGradientStop)
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, from));
            gradientStops.Insert(1, compositor.CreateColorGradientStop(1.0f, to));
        }
        else // This is almost identical to the simpler technique above.
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, from));
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.5f, BlendColors(from, to)));
            gradientStops.Insert(2, compositor.CreateColorGradientStop(1.0f, to));
        }

        // Set the direction of the gradient (top to bottom).
        gb.StartPoint = new System.Numerics.Vector2(0, 0);
        gb.EndPoint = new System.Numerics.Vector2(0, 1);

        // Bitmap and clip edges are anti-aliased.
        spriteVisual.BorderMode = Microsoft.UI.Composition.CompositionBorderMode.Soft;

        // Subtract color channels in background.
        spriteVisual.CompositeMode = Microsoft.UI.Composition.CompositionCompositeMode.MinBlend;

        #region [Sizing]
        // Set the size of the sprite visual to cover the element.
        spriteVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;
        
        //spriteVisual.Offset = new System.Numerics.Vector3(1.01f, 1.01f, 0f);
        //spriteVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(0.97f, 0.96f);

        // Or you can be more specific:
        //spriteVisual.Offset = new System.Numerics.Vector3(1f, 1f, 0f);
        //spriteVisual.Size = new System.Numerics.Vector2((float)element.ActualSize.X, (float)element.ActualSize.Y);

        // Example: Only cover the inner portion of the button
        //spriteVisual.Offset = new System.Numerics.Vector3((float)element.ActualSize.X/4, (float)element.ActualSize.Y/4, 0f);
        //spriteVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(0.5f, 0.5f);
        #endregion

        // Apply the gradient brush to the visual.
        spriteVisual.Brush = gb;

        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);
    }

    /// <summary>
    /// Scale animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementScale(double from, double to, TimeSpan duration, UIElement target, string ease, Microsoft.UI.Composition.AnimationDirection direction = Microsoft.UI.Composition.AnimationDirection.Normal)
    {
        Microsoft.UI.Composition.CompositionEasingFunction easer;
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        if (targetVisual is null) { return; }

        // Instead of calculating the parent bounds for setting the AnchorPoint, it's easier just
        // to place the button inside a Grid/StackPanel when using Vertical/Horizontal alignments.

        //targetVisual.Size = new System.Numerics.Vector2(target.ActualSize.X, target.ActualSize.Y);
        //targetVisual.AnchorPoint = new System.Numerics.Vector2(-1f, -1f);
        //targetVisual.RelativeOffsetAdjustment = new System.Numerics.Vector3(1f, 0f, 0f);
        if (_anchorPoint != System.Numerics.Vector2.Zero)
            targetVisual.AnchorPoint = _anchorPoint;

        // This is important for the effect to work properly. Alternatively, you can also set the control's RenderTransformOrigin="0.5,0.5".
        targetVisual.CenterPoint = new System.Numerics.Vector3(target.ActualSize.X / 2f, target.ActualSize.Y / 2f, 0f);

        var compositor = targetVisual.Compositor;
        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.SetToFinalValue;
        scaleAnimation.Direction = direction;
        scaleAnimation.Duration = duration;
        scaleAnimation.Target = "Scale";

        if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
            easer = compositor.CreateLinearEasingFunction();
        else
            easer = Extensions.CreatePennerEquation(compositor, ease);

        scaleAnimation.InsertKeyFrame(0.0f, new System.Numerics.Vector3((float)from), easer);
        scaleAnimation.InsertKeyFrame(1.0f, new System.Numerics.Vector3((float)to), easer);

        // Create a scoped batch so we can setup a completed event.
        var batch = targetVisual.Compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
        batch.Completed += (s, e) => { Debug.WriteLine($"[INFO] Scale animation completed for {target.GetType().Name}"); };
        targetVisual.StartAnimation("Offset", scaleAnimation);
        batch.End(); // You must call End() to get the completed event to fire.
        targetVisual.StartAnimation("Scale", scaleAnimation);
    }
    #endregion

    /// <summary>
    /// Blends two <see cref="Windows.UI.Color"/> inputs based on the given ratio.
    /// <example><code>
    ///   /* Blend 75% blue and 25% red */
    ///   Color blendMoreTowardsBlue = ColorHelper.BlendColors(Colors.Red, Colors.Blue, 0.75);
    /// </code></example>
    /// </summary>
    /// <param name="color1">The first color.</param>
    /// <param name="color2">The second color.</param>
    /// <param name="ratio">A ratio between 0 and 1. A value of 0 returns color1, and a value of 1 returns color2. Values in between blend the two colors.</param>
    /// <returns>A new <see cref="Windows.UI.Color"/> that represents the blended result.</returns>
    static Windows.UI.Color BlendColors(Windows.UI.Color color1, Windows.UI.Color color2, double ratio = 0.5)
    {
        // Ensure ratio is between 0 and 1
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        byte blendedR = (byte)((color1.R * (1 - ratio)) + (color2.R * ratio));
        byte blendedG = (byte)((color1.G * (1 - ratio)) + (color2.G * ratio));
        byte blendedB = (byte)((color1.B * (1 - ratio)) + (color2.B * ratio));
        byte blendedA = (byte)((color1.A * (1 - ratio)) + (color2.A * ratio));
        return Windows.UI.Color.FromArgb(blendedA, blendedR, blendedG, blendedB);
    }
}
