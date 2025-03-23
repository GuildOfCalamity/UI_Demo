using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;

namespace UI_Demo;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlotControl : UserControl
{
    #region [Props]
    //ToolTip? _tooltip;

    bool _loaded = false;
    bool _isDrawing = false;
    bool _sizeSet = false;

    int _msDelay = 4;
    double _restingOpacity = 0.7;
    double _circleRadius = 8;
    
    
    Storyboard? _opacityInStoryboard;
    Storyboard? _opacityOutStoryboard;
    TimeSpan _duration = TimeSpan.FromMilliseconds(600);
    
    List<int> _dataPoints = new List<int>();
    #endregion

    #region [Dependency Properties]
    /// <summary>
    ///   This is the property that triggers the plot graph for the <see cref="PlotControl"/>.
    /// </summary>
    public static readonly DependencyProperty PointSourceProperty = DependencyProperty.Register(
        nameof(PointSource),
        typeof(List<int>),
        typeof(PlotControl),
        new PropertyMetadata(null, OnPointsPropertyChanged));

    public List<int> PointSource
    {
        get => (List<int>)GetValue(PointSourceProperty);
        set => SetValue(PointSourceProperty, value);
    }
    static void OnPointsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PlotControl)d;
        if (e.NewValue is List<int> points)
            control.PointsChanged(points);
    }
    void PointsChanged(List<int> points)
    {
        if (!_loaded && points is not null)
        {
            _dataPoints = points;
            return;
        }
        else if (!_loaded || points is null)
            return;

        DrawCirclePlotDelayed(points, 0);
    }

    /// <summary>
    ///   This is the property that triggers the plot graph for the <see cref="PlotControl"/>.
    /// </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(PlotControl),
        new PropertyMetadata(string.Empty, OnTitlePropertyChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    static void OnTitlePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PlotControl)d;
        if (e.NewValue is string title)
            control.TitleChanged(title);
    }
    void TitleChanged(string title)
    {
        if (string.IsNullOrEmpty(title))
            return;

        tbTitle.Text = title;
    }
    #endregion

    public PlotControl()
    {
        this.InitializeComponent();

        //_tooltip = new ToolTip();
        
        this.Loaded += PlotControlOnLoaded;
        this.Unloaded += PlotControlOnUnloaded;
        this.SizeChanged += PlotControlOnSizeChanged;
        this.GotFocus += PlotControlOnGotFocus;
        this.LostFocus += PlotControlOnLostFocus;
    }

    public PlotControl(List<int> points) : this()
    {
        _dataPoints = points;
    }

    //protected override Size MeasureOverride(Size availableSize)
    //{
    //    Debug.WriteLine($"[EVENT] Layout cycle: {availableSize}");
    //    return base.MeasureOverride(availableSize);
    //}

    void PlotControlOnLostFocus(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[EVENT] PlotControl lost focus.");
    }

    void PlotControlOnGotFocus(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[EVENT] PlotControl got focus.");
    }

    /// <summary>
    /// This can be called many time on first render, so avoid setting the 
    /// control sizes multiple times or it may cause a layout cycle exception.
    /// </summary>
    void PlotControlOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width.IsInvalidOrZero() || e.NewSize.Height.IsInvalidOrZero())
            return;

        //Debug.WriteLine($"[EVENT] PlotControl got size change: {e.NewSize.Width},{e.NewSize.Height}");

        if (cvsPlot.Width.IsInvalidOrZero() && cvsPlot.Height.IsInvalidOrZero())
        {
            cvsPlot.Width = e.NewSize.Width - 60;
            cvsPlot.Height = e.NewSize.Height - (100 + _circleRadius);
        }
    }

    /// <summary>
    /// Draws circle plot points on the canvas.
    /// </summary>
    /// <remarks>
    /// If zero is given for the <paramref name="maxValue"/> the it will be calculated during 
    /// this method as the normalized graph offset.
    /// </remarks>
    public void DrawCirclePlot(List<int> dataPoints, int maxValue = 0)
    {
        // Clear any previous canvas plots
        cvsPlot.Children.Clear();

        if (dataPoints == null || dataPoints.Count == 0)
            return;

        if (!_sizeSet)
        {
            _sizeSet = true;
            cvsPlot.Width = this.ActualWidth - 60;
            cvsPlot.Height = this.ActualHeight - (100 + _circleRadius);
        }

        // Define Canvas size (you can also get this from the actual Canvas dimensions)
        double canvasWidth = cvsPlot.Width;
        double canvasHeight = cvsPlot.Height;

        // Check for invalid Canvas size
        if (canvasWidth.IsInvalidOrZero() || canvasHeight.IsInvalidOrZero())
        {
            Debug.WriteLine("[WARNING] Invalid canvas size.");
            return;
        }

        // If no max was defined, find the maximum value in the data to normalize the y-axis
        if (maxValue <= 0)
            maxValue = dataPoints.Max();

        // Colored brush appearance
        var circleFill = Extensions.CreateLinearGradientBrush(Colors.WhiteSmoke, Colors.DodgerBlue, Colors.MidnightBlue);
        SolidColorBrush circleStroke = new SolidColorBrush(Colors.Gray);
        double circleStrokeThickness = 2;

        // Calculate spacing between points on the x-axis
        double xSpacing = canvasWidth / (dataPoints.Count + 1); // Add 1 to count for space on left and right of chart

        _isDrawing = true;

        // Draw the circles
        for (int i = 0; i < dataPoints.Count; i++)
        {
            if (!_loaded)
                break;

            // Calculate X position based on index and spacing
            double x = (i + 1) * xSpacing; // Start plotting with an offset for readability
            if (x.IsInvalid())
                x = 0;

            // Calculate Y position based on value, maximum value and canvas height
            // Invert y axis so that higher values are at the top.
            double y = canvasHeight - (dataPoints[i] / (double)maxValue) * canvasHeight;
            if (y.IsInvalid())
                y = 0;

            // Create the circle
            Ellipse circle = new Ellipse();
            circle.Width = _circleRadius * 2;
            circle.Height = _circleRadius * 2;
            circle.Fill = circleFill;
            circle.Stroke = circleStroke;
            circle.StrokeThickness = circleStrokeThickness;
            circle.Opacity = _restingOpacity;

            // Position the circle on the canvas
            Canvas.SetLeft(circle, x - _circleRadius); // Center circle horizontally
            Canvas.SetTop(circle, y - _circleRadius);   // Center circle vertically

            // Attach tooltip data value
            circle.Tag = dataPoints[i]; // Store the data value in the circle's Tag property
            circle.PointerEntered += CircleOnPointerEntered;
            circle.PointerExited += CircleOnPointerExited;

            // Add the circle to the canvas
            cvsPlot.Children.Add(circle);
        }
        _isDrawing = false;
    }

    /// <summary>
    /// Draws circle plot points on the canvas with a small delay between each render for effect.
    /// </summary>
    /// <remarks>
    /// If zero is given for the <paramref name="maxValue"/> the it will be calculated during 
    /// this method as the normalized graph offset.
    /// </remarks>
    public void DrawCirclePlotDelayed(List<int> dataPoints, int maxValue)
    {
        // Clear and previous canvas plots
        cvsPlot.Children.Clear();

        if (dataPoints == null || dataPoints.Count == 0)
            return;

        if (!_sizeSet)
        {
            _sizeSet = true;
            cvsPlot.Width = this.ActualWidth - 60;
            cvsPlot.Height = this.ActualHeight - (100 + _circleRadius);
        }

        // Define Canvas size (you can also get this from the actual Canvas dimensions)
        double canvasWidth = cvsPlot.Width;
        double canvasHeight = cvsPlot.Height;

        // Check for invalid Canvas size
        if (canvasWidth.IsInvalidOrZero() || canvasHeight.IsInvalidOrZero())
        {
            Debug.WriteLine("[WARNING] Invalid canvas size.");
            return;
        }

        // If no max was defined, find the maximum value in the data to normalize the y-axis
        if (maxValue <= 0)
            maxValue = dataPoints.Max();

        // Colored brush appearance
        var circleFill = Extensions.CreateLinearGradientBrush(Colors.WhiteSmoke, Colors.DodgerBlue, Colors.MidnightBlue);
        SolidColorBrush circleStroke = new SolidColorBrush(Colors.Gray);
        double circleStrokeThickness = 2;

        // Calculate spacing between points on the x-axis
        double xSpacing = canvasWidth / (dataPoints.Count + 1); // Add 1 to count for space on left and right of chart

        Task.Run(async () =>
        {
            _isDrawing = true;
            for (int i = 0; i < dataPoints.Count; i++)
            {
                if (!_loaded)
                    break;

                // Calculate X position based on index and spacing
                double x = (i + 1) * xSpacing; // Start plotting with an offset for readability
                if (x.IsInvalid())
                    x = 0;

                // Calculate Y position based on value, maximum value and canvas height
                // Invert y axis so that higher values are at the top.
                double y = canvasHeight - (dataPoints[i] / (double)maxValue) * canvasHeight;
                if (y.IsInvalid())
                    y = 0;

                // Any access to a Microsoft.UI.Xaml.Controls element must be done on the dispatcher.
                cvsPlot.DispatcherQueue.TryEnqueue(() =>
                {
                    // Create the circle
                    Ellipse circle = new Ellipse();
                    circle.Width = _circleRadius * 2;
                    circle.Height = _circleRadius * 2;
                    circle.Fill = circleFill;
                    circle.Stroke = circleStroke;
                    circle.StrokeThickness = circleStrokeThickness;
                    circle.Opacity = _restingOpacity;

                    // Position the circle on the canvas
                    Canvas.SetLeft(circle, x - _circleRadius); // Center circle horizontally
                    Canvas.SetTop(circle, y - _circleRadius);   // Center circle vertically

                    // Attach tooltip data value
                    circle.Tag = dataPoints[i]; // Store the data value in the circle's Tag property
                    circle.PointerEntered += CircleOnPointerEntered;
                    circle.PointerExited += CircleOnPointerExited;

                    //circle.Shadow = Extensions.GetResource<ThemeShadow>("CommandBarFlyoutOverflowShadow");
                    //circle.Translation = new System.Numerics.Vector3(0, 0, 32);

                    // Add the circle to the canvas
                    cvsPlot.Children.Add(circle);
                });
                
                await Task.Delay(_msDelay);
            }
            _isDrawing = false;
        });
    }

    #region [Events]
    void PlotControlOnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loaded)
        {
            _loaded = true;
            cvsPlot.Margin = new Thickness(20);
            // If we received data during constructor then plot it.
            if (_dataPoints.Count > 0)
            {
                // Allow some time for the control to render before plotting.
                Task.Run(async () => { await Task.Delay(350); }).ContinueWith(t =>
                {
                    host.DispatcherQueue.TryEnqueue(() => DrawCirclePlotDelayed(_dataPoints, 0));
                });
            }
        }
    }

    void PlotControlOnUnloaded(object sender, RoutedEventArgs e) => _loaded = false;

     /// <summary>
    /// TODO: Add opacity animation to the tooltip.
    /// </summary>
    void CircleOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawing || !_loaded)
            return;

        Ellipse circle = (Ellipse)sender;
        int dataValue = (int)circle.Tag; // Retrieve the data value from the Tag

        GeneralTransform transform = circle.TransformToVisual(this); // "cvsPlot", or "root" grid, or "this" if it's a Page/UserControl
        Point position = transform.TransformPoint(new Point(circle.Width / 2, circle.Height / 2)); // Center of the circle
        Debug.WriteLine($"[INFO] Position data is X={position.X:N1}, Y={position.Y:N1}");

        #region [Didn't work properly]
        //var tooltipExample = ToolTipService.GetToolTip(circle) as ToolTip;
        //_tooltip.Content = $"Value: {dataValue}";
        //_tooltip.PlacementTarget = circle;
        //_tooltip.PlacementRect = new Rect(position.X, position.Y, 100, 40);
        //_tooltip.Placement = PlacementMode.Mouse;
        //_tooltip.HorizontalOffset = _circleRadius + 1;  // X offset from mouse
        //_tooltip.VerticalOffset = _circleRadius + 1;    // Y offset from mouse
        //ToolTipService.SetToolTip(circle, _tooltip);
        //_tooltip.IsOpen = true;
        //_tooltip.IsEnabled = true;
        #endregion

        ttValue.Text= $"Value: {dataValue}";
        ttPlot.PlacementTarget = circle;
        // Setting the placement rectangle is important when using code-behind
        ttPlot.PlacementRect = new Rect(position.X, position.Y, 100, 40);
        ttPlot.Placement = PlacementMode.Mouse;   // this behaves abnormally when not set to mouse
        ttPlot.HorizontalOffset = _circleRadius <= 10 ? _circleRadius : _circleRadius / 2; // X offset from mouse
        ttPlot.VerticalOffset = _circleRadius <= 10 ? _circleRadius : _circleRadius / 2;   // Y offset from mouse
        ttPlot.Visibility = Visibility.Visible;

        #region [Animation]
        if (_opacityInStoryboard == null)
        {
            // Create the storyboard and animation only once
            _opacityInStoryboard = new Storyboard();
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = _restingOpacity,
                To = 1.0,
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase(),
                Duration = new Duration(_duration),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            _opacityInStoryboard.Children.Add(opacityAnimation);
        }
        else
        {
            _opacityInStoryboard.Stop(); // Stop any previous animation
        }
        Storyboard.SetTarget(_opacityInStoryboard.Children[0], (Ellipse)sender); // Set the new target
        _opacityInStoryboard.Begin();
        #endregion
    }

    /// <summary>
    /// Hide the tooltip when the pointer exits the plot point.
    /// </summary>
    void CircleOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawing || !_loaded)
            return;

        ttPlot.Visibility = Visibility.Collapsed;

        #region [Animation]
        if (_opacityOutStoryboard == null)
        {
            // Create the storyboard and animation only once
            _opacityOutStoryboard = new Storyboard();
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 1.0, // From = ((Ellipse)sender).Opacity,
                To = _restingOpacity,
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase(),
                Duration = new Duration(_duration),
                //RepeatBehavior = RepeatBehavior.Forever,
            };

            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            _opacityOutStoryboard.Children.Add(opacityAnimation);
        }
        else
        {
            _opacityOutStoryboard.Stop(); // Stop any previous animation
        }
        Storyboard.SetTarget(_opacityOutStoryboard.Children[0], (Ellipse)sender); // Set the new target
        _opacityOutStoryboard.Begin();
        #endregion
    }
    #endregion
}
