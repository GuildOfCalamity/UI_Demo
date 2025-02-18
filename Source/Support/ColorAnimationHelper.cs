using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

using Windows.UI;

namespace UI_Demo;

public static class ColorAnimationHelper
{
    static Dictionary<UIElement, CompositionColorBrush> _elementBrushes = new();
    static Dictionary<UIElement, ColorKeyFrameAnimation> _elementAnimations = new();

    /// <summary>
    /// Only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    /// https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    /// <remarks>
    /// Do not call this on a <see cref="UIElement"/> that is modifying its scale property via a concurrent animation.
    /// </remarks>
    public static void CreateOrStartAnimation(UIElement? element, Color from, Color to, TimeSpan duration, string ease = "linear")
    {
        if (element == null) { return; }

        var targetVisual = ElementCompositionPreview.GetElementVisual(element);
        if (targetVisual == null) { return; }

        var prop = element?.GetValue(UIElement.VisibilityProperty);
        if (prop is null) { Debugger.Break(); } // control is not visible/loaded

        // Setup the ColorKeyFrameAnimation via the target visual's compositor.
        var compositor = targetVisual.Compositor;

        // Check if animation already exists for this element
        if (!_elementAnimations.TryGetValue(element, out var colorAnimation))
        {
            Microsoft.UI.Composition.CompositionEasingFunction easer;

            if (duration == TimeSpan.Zero || duration == TimeSpan.MinValue)
                duration = TimeSpan.FromMilliseconds(10);

            colorAnimation = compositor.CreateColorKeyFrameAnimation();
            colorAnimation.StopBehavior = AnimationStopBehavior.SetToInitialValue;
            colorAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
            colorAnimation.Duration = duration;
            colorAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            colorAnimation.Direction = AnimationDirection.Alternate;
            // Set the interpolation to go through the RGB/HSL space.
            colorAnimation.InterpolationColorSpace = CompositionColorSpace.Rgb;

            // Determine easing function.
            if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
                easer = compositor.CreateLinearEasingFunction();
            else
                easer = Extensions.CreatePennerEquation(compositor, ease);

            // Add keyframes. 1st keyframe is the start color, 2nd keyframe is the end color.
            colorAnimation.InsertKeyFrame(0, from, easer);
            colorAnimation.InsertKeyFrame(1, to, easer);

            _elementAnimations[element] = colorAnimation;
        }

        // Check if brush exists or create a new one
        if (!_elementBrushes.TryGetValue(element, out var colorBrush))
        {
            colorBrush = compositor.CreateColorBrush();
            _elementBrushes[element] = colorBrush;
            var spriteVisual = compositor.CreateSpriteVisual();
            if (spriteVisual is null) { return; }
            /*
             *   The ColorKeyFrameAnimation class is one of the supported types of KeyFrameAnimations 
             *   that is used to animate the Color property off of the Brush property on a SpriteVisual. 
             *   When working with ColorKeyFrameAnimations, utilize Windows.UI.Color objects for the 
             *   values of keyframes. Utilize the InterpolationColorSpace property to define which color 
             *   space the system will interpolate through for the animation.
             *   https://github.com/MicrosoftDocs/winrt-api/blob/docs//windows.ui.composition/colorkeyframeanimation.md
             */
            spriteVisual.CompositeMode = CompositionCompositeMode.MinBlend;
            // Set the size of the sprite visual to cover the element.
            spriteVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;
            // Or you can be more specific:
            //spriteVisual.Offset = new System.Numerics.Vector3(0.5f, 0.5f, 0f);
            //spriteVisual.Size = new System.Numerics.Vector2((float)element.ActualSize.X, (float)element.ActualSize.Y);

            // Assign the CompositionColorBrush to the sprite's visual.
            spriteVisual.Brush = colorBrush;

            // When using a sprite, the animation will not work unless the child visual is set.
            ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);
        }

        // Start the animation
        colorBrush?.StartAnimation("Color", colorAnimation);
    }

    /// <summary>
    /// Stops the <see cref="CompositionColorBrush"/> animation for a specific <see cref="UIElement"/>.
    /// </summary>
    public static void StopAnimation(UIElement? element)
    {
        if (element == null) { return; }

        if (_elementBrushes.TryGetValue(element, out var colorBrush))
        {
            colorBrush?.StopAnimation("Color");
        }
    }

    /// <summary>
    /// Stops all active animations for each <see cref="UIElement"/> in <see cref="_elementBrushes"/>.
    /// </summary>
    public static void StopAllAnimations()
    {
        foreach (var brush in _elementBrushes.Values)
        {
            brush?.StopAnimation("Color");
        }
    }
}