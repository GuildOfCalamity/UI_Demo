using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UI_Demo;

public static class ColorAnimationHelper
{
    static Dictionary<UIElement, CompositionColorBrush> _elementBrushes = new();
    static Dictionary<UIElement, ColorKeyFrameAnimation> _elementAnimations = new();

    /// <summary>
    ///   Only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    ///   https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    /// <remarks>
    ///   Do not call this on a <see cref="UIElement"/> that is modifying its scale property via a concurrent animation.
    /// </remarks>
    public static void CreateOrStartAnimation(UIElement? element, Color from, Color to, TimeSpan duration, AnimationIterationBehavior behavior = AnimationIterationBehavior.Forever, string ease = "linear")
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
            if (behavior != AnimationIterationBehavior.Forever)
            {
                colorAnimation.IterationCount = 1;
                colorAnimation.IterationBehavior = AnimationIterationBehavior.Count;
                colorAnimation.Direction = AnimationDirection.Normal;
            }
            else
            {
                colorAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
                colorAnimation.Direction = AnimationDirection.Alternate;
            }
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

            // Cover twice the area around the element (don't forget to adjust the offset).
            //spriteVisual.Scale = new System.Numerics.Vector3(2f, 2f, -1f);

            // When using a sprite, the animation will not work unless the child visual is set.
            ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);
        }

        // Start the animation
        colorBrush?.StartAnimation("Color", colorAnimation);
    }

    /// <summary>
    ///   Only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    ///   https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    /// <remarks>
    ///   Do not call this on a <see cref="UIElement"/> that is modifying its scale property via a concurrent animation.
    /// </remarks>
    public static void CreateAndStartOneTimeAnimation(UIElement? element, Color from, Color to, TimeSpan duration, string ease = "linear")
    {
        if (element == null) { return; }

        var targetVisual = ElementCompositionPreview.GetElementVisual(element);
        if (targetVisual == null) { return; }

        var prop = element?.GetValue(UIElement.VisibilityProperty);
        if (prop is null) { Debugger.Break(); } // control is not visible/loaded

        // Setup the ColorKeyFrameAnimation via the target visual's compositor.
        var compositor = targetVisual.Compositor;

        Microsoft.UI.Composition.CompositionEasingFunction easer;

        if (duration == TimeSpan.Zero || duration == TimeSpan.MinValue)
            duration = TimeSpan.FromMilliseconds(10);

        var colorAnimation = compositor.CreateColorKeyFrameAnimation();
        colorAnimation.StopBehavior = AnimationStopBehavior.SetToInitialValue;
        colorAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
        colorAnimation.Duration = duration;
        colorAnimation.IterationCount = 1;
        colorAnimation.IterationBehavior = AnimationIterationBehavior.Count;
        colorAnimation.Direction = AnimationDirection.Normal;

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

        var colorBrush = compositor.CreateColorBrush();
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

        // Cover twice the area around the element (don't forget to adjust the offset).
        //spriteVisual.Scale = new System.Numerics.Vector3(2f, 2f, -1f);

        // When using a sprite, the animation will not work unless the child visual is set.
        ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);

        // Start the animation
        colorBrush?.StartAnimation("Color", colorAnimation);
    }

    /// <summary>
    ///   Stops the <see cref="CompositionColorBrush"/> animation for a specific <see cref="UIElement"/>.
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
    ///   Stops all active animations for each <see cref="UIElement"/> in <see cref="_elementBrushes"/>.
    /// </summary>
    public static void StopAllAnimations()
    {
        foreach (var brush in _elementBrushes.Values)
        {
            brush?.StopAnimation("Color");
        }
    }

    #region [Helpers]
    /// <summary>
    /// Helper method for creating <see cref="LinearGradientBrush"/>s.
    /// </summary>
    public static LinearGradientBrush CreateLinearGradientBrush(Color c1, Color c2)
    {
        var lgb = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(0, 1)
        };
        lgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        if (!AreIdentical(c1, c2))
        {
            // Blend the two colors by 50% and inject gradient stop.
            Color blended = BlendColors(c1, c2);
            //Color multiple = BlendMultipleColors(new List<Color>() { c1, c2 }, new List<double> { 1.0, 0.8 });
            Debug.WriteLine($"[INFO] Blended color will be {blended}");
            lgb.GradientStops.Add(new GradientStop { Color = blended, Offset = 0.3 });
        }
        lgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 0.87 });
        lgb.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
        //lgb.SpreadMethod = GradientSpreadMethod.Pad;
        return lgb;
    }

    /// <summary>
    /// Helper method for creating <see cref="RadialGradientBrush"/>s.
    /// </summary>
    public static RadialGradientBrush CreateRadialGradientBrush(Color c1, Color c2)
    {
        var rgb = new RadialGradientBrush();
        rgb.Center = new Windows.Foundation.Point(0.5, 0.5);
        rgb.RadiusX = 0.5; rgb.RadiusY = 0.5;
        rgb.FallbackColor = Microsoft.UI.Colors.DodgerBlue;
        rgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        rgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 1.0 });
        return rgb;
    }

    /// <summary>
    /// Helper method for creating <see cref="RadialGradientBrush"/>s.
    /// </summary>
    public static RadialGradientBrush CreateRadialGradientBrush(Color c1, Color c2, Color c3)
    {
        var rgb = new RadialGradientBrush();
        rgb.Center = new Windows.Foundation.Point(0.5, 0.5);
        rgb.RadiusX = 0.5; rgb.RadiusY = 0.5;
        rgb.FallbackColor = Microsoft.UI.Colors.DodgerBlue;
        rgb.GradientStops.Add(new GradientStop { Color = c1, Offset = 0.0 });
        rgb.GradientStops.Add(new GradientStop { Color = c2, Offset = 0.5 });
        rgb.GradientStops.Add(new GradientStop { Color = c3, Offset = 1.0 });
        return rgb;
    }


    /// <summary>
    /// Determine if two colors are the same.
    /// </summary>
    /// <remarks>By default, the alpha channel is included in the evaluation.</remarks>
    static bool AreIdentical(Windows.UI.Color c1, Windows.UI.Color c2, bool includeAlpha = true)
    {
        if (!includeAlpha)
            return (c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);

        return (c1.A == c2.A && c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);
    }

    /// <summary>
    /// Blends two Windows.UI.Color inputs based on the given ratio.
    /// <example><code>
    ///   /* Blend 75% blue and 25% red */
    ///   Color blendMoreTowardsBlue = ColorHelper.BlendColors(Colors.Red, Colors.Blue, 0.75);
    /// </code></example>
    /// </summary>
    /// <param name="color1">The first color.</param>
    /// <param name="color2">The second color.</param>
    /// <param name="ratio">A ratio between 0 and 1. A value of 0 returns color1, and a value of 1 returns color2. Values in between blend the two colors.</param>
    /// <returns>A new <see cref="Windows.UI.Color"/> that represents the blended result.</returns>
    static Windows.UI.Color BlendColors(Color color1, Color color2, double ratio = 0.5)
    {
        // Ensure ratio is between 0 and 1
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        byte blendedR = (byte)((color1.R * (1 - ratio)) + (color2.R * ratio));
        byte blendedG = (byte)((color1.G * (1 - ratio)) + (color2.G * ratio));
        byte blendedB = (byte)((color1.B * (1 - ratio)) + (color2.B * ratio));
        byte blendedA = (byte)((color1.A * (1 - ratio)) + (color2.A * ratio));
        return Windows.UI.Color.FromArgb(blendedA, blendedR, blendedG, blendedB);
    }
    #endregion
}