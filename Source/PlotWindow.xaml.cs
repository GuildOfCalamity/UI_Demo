using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI;
using System.Threading.Tasks;

using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Hosting;

using Windows.Foundation;
using Windows.UI.WindowManagement;

namespace UI_Demo;

public sealed partial class PlotWindow : Window
{
    #region [Props]
    ContentCoordinateConverter _coordinateConverter;
    OverlappedPresenter? _overlapPresenter;
    Microsoft.UI.Windowing.AppWindow? _appWindow;

    bool _loaded = false;
    bool _isDrawing = false;
    int _msDelay = 10;
    int _maxCeiling = 110000;
    double _circleRadius = 16;
    double _restingOpacity = 0.625;

    Storyboard? _opacityInStoryboard;
    Storyboard? _opacityOutStoryboard;
    TimeSpan _duration = TimeSpan.FromMilliseconds(600);

    List<int> _dataPoints = new List<int>();
    List<string> _sizes = new List<string>() 
    { 
        "4", "6", "8", "10", "12", "16", "20", "24", "30", "50" 
    };
    List<string> _delays = new List<string>() 
    { 
        "2", "10", "20", "40", "60", "80" 
    };
    List<string> _types = new List<string>()
    {
        "Linear (slope 1)", "Linear (slope 2)",
        "Bell Curve (deviation 1)", "Bell Curve (deviation 2)", "Bell Curve (range alt)",
        "Quadratic", "Quadratic (reverse)",
        "Cubic", "Quartic", "Quintic",
        "Sine Wave", "Cosine Wave", "Tangent Wave",
        "Gradient Mapping", 
        "Logistic Function",
        "Rose Curve (dual-plot)", "Lissajous Curve (dual-plot)",
        "SawTooth Wave", "Square Wave", "Square Wave (rounded)",
        "Logarithmic (base 1)", "Logarithmic (base 2)"
    };
    #endregion

    public PlotWindow()
    {
        this.InitializeComponent();
        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
            this.ExtendsContentIntoTitleBar = true;
            //this.AppWindow.DefaultTitleBarShouldMatchAppModeTheme = true;
            this.AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Standard;
            SetTitleBar(CustomTitleBar);
        }

        CreateGradientBackdrop(root, new System.Numerics.Vector2(0.9f, 1));

        // For programmatic minimize/maximize/restore
        _overlapPresenter = AppWindow.Presenter as OverlappedPresenter;
        // For translating screen to local Windows.Foundation.Point
        _coordinateConverter = ContentCoordinateConverter.CreateForWindowId(AppWindow.Id);

        cmbTypes.ItemsSource = _types;
        cmbTypes.SelectedItem = _types[0];
        cmbTypes.SelectionChanged += TypesOnSelectionChanged;

        cmbSizes.ItemsSource = _sizes;
        cmbSizes.SelectedItem = _sizes[5];
        cmbSizes.SelectionChanged += SizesOnSelectionChanged;

        cmbDelay.ItemsSource = _delays;
        cmbDelay.SelectedItem = _delays[1];
        cmbDelay.SelectionChanged += DelayOnSelectionChanged;

        this.Activated += PlotWindowOnActivated;
        this.Closed += PlotWindowOnClosed;
        //this.SizeChanged += PlotWindowOnSizeChanged; // We're handling the resize event in the CreateGradientBackdrop().

        #region [AppWindow and Icon]
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd); // Retrieve the WindowId that corresponds to hWnd.
        _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId); // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
        if (_appWindow is not null)
        {
            if (App.IsPackaged)
                _appWindow?.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Assets/AppIcon.ico"));
            else
                _appWindow?.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, $"Assets/AppIcon.ico"));
        }
        #endregion

    }

    /// <summary>
    /// You only need to pass the Y points, the X points (graph over time) 
    /// will be calculated automatically based on the size of the window.
    /// </summary>
    /// <param name="points"><see cref="List{Int32}"/></param>
    public PlotWindow(List<int> points) : this()
    {
        _dataPoints = points;
    }

    #region [Events]
    void TypesOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loaded)
            return;

        if (sender is not null)
            RunSelection(((ComboBox)sender).SelectedValue as string ?? "");
    }

    void SizesOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loaded)
            return;

        if (sender is not null)
            double.TryParse(((ComboBox)sender).SelectedValue as string, out _circleRadius);
    }

    void DelayOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_loaded)
            return;

        if (sender is not null)
            int.TryParse(((ComboBox)sender).SelectedValue as string, out _msDelay);
    }


    /// <summary>
    /// We're handling the resize event in the CreateGradientBackdrop().
    /// </summary>
    void PlotWindowOnSizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        if (args.Size.Width.IsInvalid() || args.Size.Height.IsInvalid() || args.Size.Width == 0 || args.Size.Height == 0)
                return;

        cvsPlot.Width = args.Size.Width - 80;
        cvsPlot.Height = args.Size.Height - (120 + _circleRadius);
    }

    void PlotWindowOnActivated(object sender, WindowActivatedEventArgs args)
    {
        bool useAppAware = true;

        if (!_loaded)
        {
            ((Window)sender).Title = "Plotter";
            cvsPlot.Margin = new Thickness(20, -10, 20, 50);
            cvsPlot.Width = ((Window)sender).Bounds.Width - 80;
            cvsPlot.Height = ((Window)sender).Bounds.Height - (120 + _circleRadius);
            if (useAppAware && args.WindowActivationState != WindowActivationState.Deactivated)
            {
                _appWindow?.MoveAndResize(new Windows.Graphics.RectInt32(App.Profile.WindowLeft - 20, App.Profile.WindowTop - 20, App.Profile.WindowWidth + 40, App.Profile.WindowHeight + 40), Microsoft.UI.Windowing.DisplayArea.Primary);
            }
            else if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                _appWindow?.Resize(new Windows.Graphics.SizeInt32(1000, 700));
                App.CenterWindow(this);
            }
            _loaded = true;

            // If we received data during constructor then plot it.
            if (_dataPoints.Count > 0)
            {
                DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
            }
        }
    }
    void PlotWindowOnClosed(object sender, WindowEventArgs args)
    {
        Debug.WriteLine("[INFO] PlotWindow closed");
    }

    /// <summary>
    /// TODO: Add opacity animation to the tooltip.
    /// </summary>
    void CircleOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawing)
            return;

        Ellipse circle = (Ellipse)sender;
        int dataValue = (int)circle.Tag; // Retrieve the data value from the Tag

        // UIElement.TransformToVisual() will not accept a Microsoft.UI.Xaml.Window
        GeneralTransform transform = circle.TransformToVisual(cvsPlot); // or "root" grid, or "this" if it's a Page/UserControl
        Point position = transform.TransformPoint(new Point(circle.Width / 2, circle.Height / 2)); // Center of the circle
        Debug.WriteLine($"[INFO] Position data is X={position.X:N1}, Y={position.Y:N1}");

        ttValue.Text = $"Value: {dataValue}";
        ttPlot.PlacementTarget = circle;
        // Setting the placement rectangle is important when using code-behind
        ttPlot.PlacementRect = new Rect(position.X, position.Y, 100, 40);
        ttPlot.Placement = PlacementMode.Mouse; // this behaves abnormally when not set to mouse
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
        if (_isDrawing)
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

    void RunSelection(string type)
    {
        _dataPoints.Clear();
        switch (type)
        {
            case "Linear (slope 1)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)PlotFunctionHelper.LinearFunction(i, 3, 5), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Linear (slope 2)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)PlotFunctionHelper.LinearFunction(i, 5, 5), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Bell Curve (deviation 1)":
                {
                    double upShift = 2500; // for graph offset since values will be tiny
                    var points = PlotFunctionHelper.BellCurveFunction(5, 1.725, 100, 5);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Bell Curve (deviation 2)":
                {
                    double upShift = 2500; // for graph offset since values will be tiny
                    var points = PlotFunctionHelper.BellCurveFunction(5, 3.125, 100, 5);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Bell Curve (range alt)":
                {
                    double upShift = 2500; // for graph offset since values will be tiny
                    var points = PlotFunctionHelper.BellCurveFunction(5, 2.25, 100, 14);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "SawTooth Wave":
                {
                    var points = PlotFunctionHelper.SawtoothWaveFunction(2.5, 150, 100, 2, 150);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)point, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Square Wave":
                {
                    var points = PlotFunctionHelper.SquareWaveFunction(3, 250, 100, 2, 350);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)point, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 2000);
                }
                break;
            case "Square Wave (rounded)":
                {
                    var points = PlotFunctionHelper.SquareWaveRoundedFunction(2, 250, 100, 2, 3, 350);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)point, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 2000);
                }
                break;
            case "Quadratic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInQuadratic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Quadratic (reverse)":
                {
                    for (int i = 101; i > 0; i--)
                    {
                        _dataPoints.Add(Math.Min(i * i, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Cubic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInCubic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Quartic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInQuartic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Quintic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInQuintic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Sine Wave":
                {
                    double upShift = 250; // for graph offset since sine values will run negative
                    var points = PlotFunctionHelper.SineWaveFunction(0.225, 145, 21);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 2));
                }
                break;
            case "Cosine Wave":
                {
                    double upShift = 250; // for graph offset since cosine values will run negative
                    var points = PlotFunctionHelper.CosineWaveFunction(0.225, 145, 21);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 2));
                }
                break;
            case "Tangent Wave":
                {
                    double upShift = 250; // for graph offset since cosine values will run negative
                    var points = PlotFunctionHelper.TangentWaveFunction(0.225, 145, 21);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 9));
                }
                break;
            case "Gradient Mapping":
                {
                    double upShift = 200;
                    var points = PlotFunctionHelper.GradientMappingFunction(0.225, 600, 11);
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 5));
                }
                break;
            case "Logistic Function":
                {
                    double upShift = 200;
                    var points = PlotFunctionHelper.LogisticFunction();
                    foreach (var point in points)
                    {
                        _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 5));
                }
                break;
            case "Rose Curve (dual-plot)":
                {
                    double upShift = 500;
                    var points = PlotFunctionHelper.GenerateRoseCurve();
                    //foreach (var point in points.yValues) { _dataPoints.Add(Math.Min((int)(point + upShift), _maxCeiling)); }
                    int total = points.yValues.Count();
                    for (int i = 0; i < total; i++)
                    {
                        if (i % 2 == 0) // alternate the X and Y points
                            _dataPoints.Add(Math.Min((int)(points.xValues[i] + upShift), _maxCeiling));
                        else
                            _dataPoints.Add(Math.Min((int)(points.yValues[i] + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 2));
                }
                break;
            case "Lissajous Curve (dual-plot)":
                {
                    double upShift = 500;
                    var points = PlotFunctionHelper.GenerateLissajousCurve();
                    int total = points.yValues.Count();
                    for (int i = 0; i < total; i++)
                    {
                        if (i % 2 == 0) // alternate the X and Y points
                            _dataPoints.Add(Math.Min((int)(points.xValues[i] + upShift), _maxCeiling));
                        else
                            _dataPoints.Add(Math.Min((int)(points.yValues[i] + upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 2));
                }
                break;
            case "Logarithmic (base 1)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        var newPoint = (int)Math.Floor(Math.Log(i, 1.0111));
                        _dataPoints.Add(newPoint);
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
            case "Logarithmic (base 2)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        var newPoint = (int)Math.Floor(Math.Log(i, 1.251));
                        _dataPoints.Add(newPoint);
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                }
                break;
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

        // Define Canvas size (you can also get this from the actual Canvas dimensions)
        double canvasWidth = cvsPlot.Width;
        double canvasHeight = cvsPlot.Height;

        // Check for invalid Canvas size
        if (canvasWidth.IsInvalid() || canvasWidth <= 0 || canvasHeight.IsInvalid() || canvasHeight <= 0)
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
    public void DrawCirclePlotDelayed(List<int> dataPoints, Control sender, int maxValue)
    {
        // Clear and previous canvas plots
        cvsPlot.Children.Clear();

        if (dataPoints == null || dataPoints.Count == 0)
            return;

        // Define Canvas size (you can also get this from the actual Canvas dimensions)
        double canvasWidth = cvsPlot.Width;
        double canvasHeight = cvsPlot.Height;

        // Check for invalid Canvas size
        if (canvasWidth.IsInvalid() || canvasWidth <= 0 || canvasHeight.IsInvalid() || canvasHeight <= 0)
        {
            Debug.WriteLine("[WARNING] Invalid canvas size.");
            return;
        }

        // Disable the sender while we work
        sender.IsEnabled = false;

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

                    if (i < dataPoints.Count)
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

            sender.DispatcherQueue.TryEnqueue(() => sender.IsEnabled = true);
        });
    }


    #region [Extras]
    public void BeginStoryboard()
    {
        if (App.AnimationsEffectsEnabled)
            OpacityStoryboard.Begin();
    }

    public void EndStoryboard()
    {
        if (App.AnimationsEffectsEnabled)
            OpacityStoryboard.SkipToFill(); //OpacityStoryboard.Stop();
    }

    void CreateGradientBackdrop(FrameworkElement fe, System.Numerics.Vector2 endPoint)
    {
        // Get the FrameworkElement's compositor.
        var compositor = ElementCompositionPreview.GetElementVisual(fe).Compositor;
        if (compositor == null) { return; }
        var gb = compositor.CreateLinearGradientBrush();

        // Define gradient stops.
        var gradientStops = gb.ColorStops;

        // If we found our App.xaml brushes then use them.
        if (App.Current.Resources.TryGetValue("GC1", out object clr1) &&
            App.Current.Resources.TryGetValue("GC2", out object clr2) &&
            App.Current.Resources.TryGetValue("GC3", out object clr3) &&
            App.Current.Resources.TryGetValue("GC4", out object clr4))
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, (Windows.UI.Color)clr1));
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, (Windows.UI.Color)clr2));
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, (Windows.UI.Color)clr3));
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, (Windows.UI.Color)clr4));
        }
        else
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, Windows.UI.Color.FromArgb(55, 255, 0, 0)));   // Red
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, Windows.UI.Color.FromArgb(55, 255, 216, 0))); // Yellow
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, Windows.UI.Color.FromArgb(55, 0, 255, 0)));   // Green
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, Windows.UI.Color.FromArgb(55, 0, 0, 255)));   // Blue
        }

        // Set the direction of the gradient.
        gb.StartPoint = new System.Numerics.Vector2(0, 0);
        //gb.EndPoint = new System.Numerics.Vector2(1, 1);
        gb.EndPoint = endPoint;

        // Create a sprite visual and assign the gradient brush.
        var spriteVisual = Compositor.CreateSpriteVisual();
        spriteVisual.Brush = gb;

        // Set the size of the sprite visual to cover the entire window.
        spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualSize.X, (float)fe.ActualSize.Y);

        // Handle the SizeChanged event to adjust the size of the sprite visual when the window is resized.
        fe.SizeChanged += (s, e) =>
        {
            spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualWidth, (float)fe.ActualHeight);

            if (e.NewSize.Width.IsInvalid() || e.NewSize.Height.IsInvalid() || e.NewSize.Width == 0 || e.NewSize.Height == 0)
                return;

            cvsPlot.Width = e.NewSize.Width - 80;
            cvsPlot.Height = e.NewSize.Height - (120 + _circleRadius);
        };

        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(fe, spriteVisual);
    }

    public void ApplyLanguageFont(TextBlock textBlock, string language) => ApplyLanguageFont(textBlock, new Windows.Globalization.Fonts.LanguageFontGroup(language).UITextFont);
    public void ApplyLanguageFont(TextBlock textBlock, Windows.Globalization.Fonts.LanguageFont? langFont)
    {
        if (langFont == null)
        {
            var langFontGroup = new Windows.Globalization.Fonts.LanguageFontGroup("en-US");
            langFont = langFontGroup.UITextFont;
        }
        FontFamily fontFamily = new FontFamily(langFont.FontFamily);
        textBlock.FontFamily = fontFamily;
        textBlock.FontWeight = langFont.FontWeight;
        textBlock.FontStyle = langFont.FontStyle;
        textBlock.FontStretch = langFont.FontStretch;
    }
    #endregion
}
