using System;
using System.Diagnostics;
using System.Numerics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace UI_Demo;

public static class BloomHelper
{
    static readonly Windows.UI.Color _defaultColor = Windows.UI.Color.FromArgb(180, 255, 255, 255);

    /// <summary>
    ///   A down-n-dirty bloom effect for any <see cref="UIElement"/> that has a <see cref="Grid"/> parent.
    ///   This defeats any existing animations the control has because ExpressionAnimations are created 
    ///   to facilitate the bloom effect via the control's <see cref="Microsoft.UI.Composition.VisualCollection"/>.
    ///   Animations may still occur if they are internally performed by the control (e.g. a custom control).
    /// </summary>
    /// <remarks>
    ///   This can be applied multiple times to the <see cref="UIElement"/> for a stronger effect.
    /// </remarks>
    public static void AddBloom(UIElement element, UIElement parent, Windows.UI.Color color, Vector3 offset, float blurRadius = 10)
    {
        if (element == null || parent == null)
        {
            Debug.WriteLine($"[WARNING] AddBloom: One (or more) UIElement is null, cannot continue.");
            return;
        }

        if (color == Microsoft.UI.Colors.Transparent)
            color = _defaultColor;

        // We're making a copy of the parent and then applying the bloom effect to its sibling.
        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.Opacity = 0;
        var compositor = visual.Compositor;

        var sizeBind = compositor.CreateExpressionAnimation("visual.Size");
        sizeBind.SetReferenceParameter("visual", visual);

        var offsetBind = compositor.CreateExpressionAnimation("visual.Offset");
        offsetBind.SetReferenceParameter("visual", visual);

        var rVisual = compositor.CreateRedirectVisual(visual);
        rVisual.StartAnimation("Size", sizeBind);

        var lVisual = compositor.CreateLayerVisual();
        lVisual.StartAnimation("Size", sizeBind);
        lVisual.StartAnimation("Offset", offsetBind);

        lVisual.Children.InsertAtTop(rVisual);

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = blurRadius;
        shadow.Color = color;
        shadow.Offset = offset;
        shadow.SourcePolicy = Microsoft.UI.Composition.CompositionDropShadowSourcePolicy.InheritFromVisualContent;

        // Set the LayerVisual's shadow and opacity.
        lVisual.Shadow = shadow;
        lVisual.Opacity = (float)element.Opacity;

        var parentContainerVisual = ElementCompositionPreview.GetElementChildVisual(parent) as Microsoft.UI.Composition.ContainerVisual;
        // Create a visual if no parent container visual exists.
        if (parentContainerVisual == null)
        {
            parentContainerVisual = compositor.CreateContainerVisual();
            parentContainerVisual.RelativeSizeAdjustment = Vector2.One;
            ElementCompositionPreview.SetElementChildVisual(parent, parentContainerVisual);
        }
        // Insert the visual at the top of the collection.
        parentContainerVisual.Children.InsertAtTop(lVisual);
    }
    public static void AddBloom(UIElement element, UIElement parent, float blurRadius = 10) => AddBloom(element, parent, _defaultColor, Vector3.Zero, blurRadius);
    public static void AddBloom(UIElement element, UIElement parent, Windows.UI.Color color, float blurRadius = 10) => AddBloom(element, parent, color, Vector3.Zero, blurRadius);

    /// <summary>
    ///   Removes the bloom effect from the specified <see cref="UIElement"/>. If <paramref name="layerVisual"/> 
    ///   is null, all <see cref="Microsoft.UI.Composition.Visual"/>s will be removed from the parent container.
    /// </summary>
    public static void RemoveBloom(UIElement element, UIElement parent, Microsoft.UI.Composition.LayerVisual? layerVisual)
    {
        if (element == null || parent == null)
            return;

        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.Opacity = (float)element.Opacity;
        var parentContainerVisual = ElementCompositionPreview.GetElementChildVisual(parent) as Microsoft.UI.Composition.ContainerVisual;
        if (parentContainerVisual != null)
        {
            // Remove the given visual or remove all visuals.
            if (layerVisual is not null)
            {
                parentContainerVisual.Children.Remove(layerVisual);
            }
            else
            {
                foreach (var vis in parentContainerVisual.Children)
                {
                    parentContainerVisual.Children.Remove(vis);
                }
            }
        }
    }

}
