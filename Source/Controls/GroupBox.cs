﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace UI_Demo;

/// <summary>
/// Portions of this code from AssyntSoftware.
/// https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh465374(v=win.10)#specifying-the-visual-structure-of-a-control
/// https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh465381(v=win.10)#style-basics
/// </summary>
[TemplatePart(Name = "PART_BorderPath", Type = typeof(Microsoft.UI.Xaml.Shapes.Path))]
[TemplatePart(Name = "PART_HeadingPresenter", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_ChildPresenter", Type = typeof(ContentPresenter))]
public sealed partial class GroupBox : ContentControl
{
    bool EnableBloomEffect { get; set; } = true;
    ContentPresenter? HeadingPresenter { get; set; }
    ContentPresenter? ChildPresenter { get; set; }
    Microsoft.UI.Xaml.Shapes.Path? BorderPath { get; set; }

    /// <summary>
    /// Don't forget to add a styler to the App.xaml
    /// </summary>
    public GroupBox()
    {
        this.DefaultStyleKey = typeof(GroupBox);
        this.PointerEntered += GroupBox_PointerEntered;
        this.PointerExited += GroupBox_PointerExited;
    }

    #region [Bloom Effect]
    void GroupBox_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (EnableBloomEffect)
        {
            var ceParent = BloomHelper.FindParentPanel((UIElement)sender);
            if (ceParent is not null)
                BloomHelper.AddBloom((UIElement)sender, ceParent, Windows.UI.Color.FromArgb(230, 25, 180, 255), 8);
        }
    }

    void GroupBox_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (EnableBloomEffect)
        {
            var ceParent = BloomHelper.FindParentPanel((UIElement)sender);
            if (ceParent is not null)
                BloomHelper.RemoveBloom((UIElement)sender, ceParent, null);
        }
    }
    #endregion

    #region [Overrides]
    /// <summary>
    /// Entry method linked to styler.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        HeadingPresenter = GetTemplateChild("PART_HeadingPresenter") as ContentPresenter ?? throw new InvalidOperationException($"{nameof(ActiveImage)}: Style template missing 'PART_HeadingPresenter', cannot continue.");
        ChildPresenter = GetTemplateChild("PART_ChildPresenter") as ContentPresenter ?? throw new InvalidOperationException($"{nameof(ActiveImage)}: Style template missing 'PART_ChildPresenter', cannot continue.");
        BorderPath = GetTemplateChild("PART_BorderPath") as Microsoft.UI.Xaml.Shapes.Path ?? throw new InvalidOperationException($"{nameof(ActiveImage)}: Style template missing 'PART_BorderPath', cannot continue.");

        // If these are always null, make sure you have a default styler in the project for the type GroupBox.
        if (HeadingPresenter is null || ChildPresenter is null || BorderPath is null)
            return;

        // offset the heading presenter from the control edge
        HeadingPresenter.Margin = new Thickness(HeadingMargin, 0, 0, 0);

        // reuse Control properties to define the group border
        RegisterPropertyChangedCallback(CornerRadiusProperty, (s, d) => ((GroupBox)s).BorderPropertyChanged());
        RegisterPropertyChangedCallback(BorderThicknessProperty, (s, d) => ((GroupBox)s).BorderPropertyChanged());

        HeadingPresenter.SizeChanged += (s, e) =>
        {
            if (IsLoaded)
                BorderPropertyChanged();
        };

        SizeChanged += (s, e) => ((GroupBox)s).RedrawBorder();

        // initialise
        Loaded += (s, e) => BorderPropertyChanged();
    }

    /// <summary>
    /// Support for UI automation.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupBoxAutomationPeer(this);
    #endregion

    #region [Dependency Properties]
    public static readonly DependencyProperty HeadingProperty =
        DependencyProperty.Register(nameof(Heading),
            typeof(object),
            typeof(GroupBox),
            new PropertyMetadata(null));
    public object Heading
    {
        get { return GetValue(HeadingProperty); }
        set { SetValue(HeadingProperty, value); }
    }


    public static readonly DependencyProperty HeadingTemplateProperty =
        DependencyProperty.Register(nameof(HeadingTemplate),
            typeof(DataTemplate),
            typeof(GroupBox),
            new PropertyMetadata(null));
    public object HeadingTemplate
    {
        get { return (DataTemplate)GetValue(HeadingTemplateProperty); }
        set { SetValue(HeadingTemplateProperty, value); }
    }


    public static readonly DependencyProperty HeadingTemplateSelectorProperty =
        DependencyProperty.Register(nameof(HeadingTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(GroupBox),
            new PropertyMetadata(null));
    public object HeadingTemplateSelector
    {
        get { return (DataTemplateSelector)GetValue(HeadingTemplateSelectorProperty); }
        set { SetValue(HeadingTemplateSelectorProperty, value); }
    }

    public static readonly DependencyProperty HeadingBaseLineRatioProperty =
        DependencyProperty.Register(nameof(HeadingBaseLineRatio),
            typeof(double),
            typeof(GroupBox),
            new PropertyMetadata(0.61, (d, e) => ((GroupBox)d).BorderPropertyChanged()));
    /// <summary>
    /// How far down the heading the border line is drawn.
    /// If 0.0, it'll be at the top of the content. 
    /// If 1.0, it would be drawn at the bottom.
    /// </summary>
    public double HeadingBaseLineRatio
    {
        get { return (double)GetValue(HeadingBaseLineRatioProperty); }
        set { SetValue(HeadingBaseLineRatioProperty, value); }
    }

    public static readonly DependencyProperty HeadingMarginProperty =
        DependencyProperty.Register(nameof(HeadingMargin),
            typeof(double),
            typeof(GroupBox),
            new PropertyMetadata(16.0, HeadingMarginPropertyChanged));
    /// <summary>
    /// The offset from this control's edge to the start of the heading presenter.
    /// </summary>
    public double HeadingMargin
    {
        get { return (double)GetValue(HeadingMarginProperty); }
        set { SetValue(HeadingMarginProperty, value); }
    }

    public static readonly DependencyProperty BorderEndPaddingProperty =
        DependencyProperty.Register(nameof(BorderEndPadding),
            typeof(double),
            typeof(GroupBox),
            new PropertyMetadata(3.0, (d, e) => ((GroupBox)d).RedrawBorder()));
    /// <summary>
    /// Padding between the end of the border and the start of the heading.
    /// This affects the border, changes won't cause a new measure pass.
    /// </summary>
    public double BorderEndPadding
    {
        get { return (double)GetValue(BorderEndPaddingProperty); }
        set { SetValue(BorderEndPaddingProperty, value); }
    }

    public static readonly DependencyProperty BorderStartPaddingProperty =
        DependencyProperty.Register(nameof(BorderStartPadding),
            typeof(double),
            typeof(GroupBox),
            new PropertyMetadata(4.0, (d, e) => ((GroupBox)d).RedrawBorder()));
    /// <summary>
    /// Padding between the start of the border and the end of the heading.
    /// This affects the border, changes won't cause a new measure pass.
    /// </summary>
    public double BorderStartPadding
    {
        get { return (double)GetValue(BorderStartPaddingProperty); }
        set { SetValue(BorderStartPaddingProperty, value); }
    }
    #endregion

    #region [Callbacks & Helpers]
    static void HeadingMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        GroupBox gb = (GroupBox)d;
        if (gb.HeadingPresenter is not null)
        {
            gb.HeadingPresenter.Margin = new Thickness((double)e.NewValue, 0, 0, 0);
            gb.RedrawBorder();
        }
    }

    void RedrawBorder()
    {
        if (IsLoaded)
            CreateBorderRoundedRect();
    }

    void BorderPropertyChanged()
    {
        if (ChildPresenter is null || BorderPath is null)
            return;

        Thickness newPadding = CalculateContentPresenterPadding();

        if (ChildPresenter.Padding != newPadding)
            ChildPresenter.Padding = newPadding;

        // a non uniform border thickness isn't supported
        if (BorderPath.StrokeThickness != BorderThickness.Left)
            BorderPath.StrokeThickness = BorderThickness.Left;

        // it's difficult to tell if changing the child presenter padding would
        // cause a size changed event, so always redraw the border here
        RedrawBorder();
    }

    Thickness CalculateContentPresenterPadding()
    {
        static double Max(double a, double b, double c) => Math.Max(Math.Max(a, b), c);

        double halfStrokeThickness = BorderThickness.Left / 2;
        double headingHeight = (HeadingPresenter is null) ? 0.0 : HeadingPresenter.ActualHeight;
        double headingBaseLineRatio = Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0);

        // if "borderOffset" is positive, the top border line extends below the top of the content presenter
        double borderOffset = -(headingHeight - ((headingHeight * headingBaseLineRatio) + halfStrokeThickness));
        double cornerAdjustment = Math.Max(CornerRadius.TopLeft, CornerRadius.TopRight) - BorderThickness.Left;
        double topPadding = cornerAdjustment + borderOffset;

        if (topPadding < borderOffset)  // top padding cannot be less that the bottom of the border 
            topPadding = borderOffset;

        if (topPadding < 0) // the content cannot be outside of the content presenter (even if that's a valid operation)
            topPadding = 0;

        // a non uniform corner radius is unlikely, but possible
        // a non uniform border thickness isn't supported
        return new Thickness(Max(CornerRadius.TopLeft, CornerRadius.BottomLeft, BorderThickness.Left),
                            topPadding,
                            Max(CornerRadius.TopRight, CornerRadius.BottomRight, BorderThickness.Left),
                            Max(CornerRadius.BottomLeft, CornerRadius.BottomRight, BorderThickness.Left));
    }

    void CreateBorderRoundedRect()
    {
        if (HeadingPresenter is null || BorderPath is null)
            return;

        static LineSegment LineTo(float x, float y) => new LineSegment() { Point = new Point(x, y), };
        static ArcSegment ArcTo(Point end, float radius) => new ArcSegment() { Point = end, RotationAngle = 90.0, IsLargeArc = false, Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise };

        PathFigure figure = new PathFigure() { IsClosed = false, IsFilled = false, };

        PathGeometry pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(figure);

        float textLHS = (float)(HeadingMargin - BorderEndPadding);
        float textRHS = (float)(HeadingMargin + HeadingPresenter.ActualWidth + BorderStartPadding);

        float halfStrokeThickness = (float)(BorderPath.StrokeThickness * 0.5);
        float headingCenter = (float)(HeadingPresenter.ActualHeight * Math.Clamp(HeadingBaseLineRatio, 0.0, 1.0));

        // right hand side of text
        float radius = (float)CornerRadius.TopRight;
        float xArcStart = ActualSize.X - (radius + halfStrokeThickness);

        if (textRHS < xArcStart) // check the first line is required, otherwise start at the arc
        {
            figure.StartPoint = new Point(textRHS, headingCenter);
            figure.Segments.Add(LineTo(xArcStart, headingCenter));
        }
        else
            figure.StartPoint = new Point(xArcStart, headingCenter);

        if (radius > 0) // top right corner
        {
            Point arcEnd = new Point(ActualSize.X - halfStrokeThickness, headingCenter + radius);
            figure.Segments.Add(ArcTo(arcEnd, radius));
        }

        radius = (float)CornerRadius.BottomRight;
        figure.Segments.Add(LineTo(ActualSize.X - halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness)));

        if (radius > 0) // bottom right corner
        {
            Point arcEnd = new Point(ActualSize.X - (radius + halfStrokeThickness), ActualSize.Y - halfStrokeThickness);
            figure.Segments.Add(ArcTo(arcEnd, radius));
        }

        radius = (float)CornerRadius.BottomLeft;
        figure.Segments.Add(LineTo(radius + halfStrokeThickness, ActualSize.Y - halfStrokeThickness));

        if (radius > 0) // bottom left corner
        {
            Point arcEnd = new Point(halfStrokeThickness, ActualSize.Y - (radius + halfStrokeThickness));
            figure.Segments.Add(ArcTo(arcEnd, radius));
        }

        radius = (float)CornerRadius.TopLeft;
        figure.Segments.Add(LineTo(halfStrokeThickness, headingCenter + radius));

        if (radius > 0) // top left corner
        {
            Point arcEnd = new Point(radius + halfStrokeThickness, headingCenter);
            figure.Segments.Add(ArcTo(arcEnd, radius));
        }

        // check if the last line is required, the arc may be too large
        if (radius + halfStrokeThickness < textLHS)
            figure.Segments.Add(LineTo(textLHS, headingCenter));

        // add the new path geometry in to the visual tree
        BorderPath.Data = pathGeometry;
    }
    #endregion
}

/// <summary>
/// Support for UI automation.
/// </summary>
public class GroupBoxAutomationPeer : FrameworkElementAutomationPeer
{
    public GroupBoxAutomationPeer(GroupBox control) : base(control) { }
    protected override string GetClassNameCore() => "GroupBox";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override string GetNameCore()
    {
        if (((GroupBox)Owner).Heading is string str)
            return str;

        return base.GetNameCore();
    }
}
