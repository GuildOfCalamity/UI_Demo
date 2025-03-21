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
public sealed partial class PlotPage : Page
{
    #region [Props]
    //ToolTip? _tooltip;

    bool _loaded = false;
    bool _isDrawing = false;
    
    double _restingOpacity = 0.7;
    double _circleRadius = 10;
    
    int _msDelay = 8;
    int _maxCeiling = 110000;
    
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
        "Sinusoidal", "SawTooth Wave", "Square Wave", "Square Wave (rounded)",
        "Logarithmic (base 1)", "Logarithmic (base 2)"
    };
    #endregion

    public PlotPage()
    {
        this.InitializeComponent();

        //_tooltip = new ToolTip();

        cmbTypes.ItemsSource = _types;
        cmbTypes.SelectedItem = _types[0];
        cmbTypes.SelectionChanged += TypesOnSelectionChanged;

        cmbSizes.ItemsSource = _sizes;
        cmbSizes.SelectedItem = _sizes[5];
        cmbSizes.SelectionChanged += SizesOnSelectionChanged;

        cmbDelay.ItemsSource = _delays;
        cmbDelay.SelectedItem = _delays[1];
        cmbDelay.SelectionChanged += DelayOnSelectionChanged;
        
        this.Loaded += PlotPageOnLoaded;
        this.SizeChanged += PlotPageOnSizeChanged;
    }

    void PlotPageOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width.IsInvalid() || e.NewSize.Height.IsInvalid())
            return;

        cvsPlot.Width = e.NewSize.Width - 80;
        cvsPlot.Height = e.NewSize.Height - (120 + _circleRadius);
    }

    public PlotPage(List<int> points) : this()
    {
        _dataPoints = points;
    }

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
                } break;
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
                    var bcPoints = PlotFunctionHelper.BellCurveFunction(5, 1.725, 100, 5);
                    foreach (var point in bcPoints)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Bell Curve (deviation 2)":
                {
                    double upShift = 2500; // for graph offset since values will be tiny
                    var bcPoints = PlotFunctionHelper.BellCurveFunction(5, 3.125, 100, 5);
                    foreach (var point in bcPoints)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Bell Curve (range alt)":
                {
                    double upShift = 2500; // for graph offset since values will be tiny
                    var bcPoints = PlotFunctionHelper.BellCurveFunction(5, 2.25, 100, 14);
                    foreach (var point in bcPoints)
                    {
                        _dataPoints.Add(Math.Min((int)(point * upShift), _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "SawTooth Wave":
                {
                    var stPoints = PlotFunctionHelper.SawtoothWaveFunction(2.5, 150, 100, 2, 150);
                    foreach (var point in stPoints)
                    {
                        _dataPoints.Add(Math.Min((int)point, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 600);
                }
                break;
            case "Square Wave":
                {
                    var swPoints = PlotFunctionHelper.SquareWaveFunction(3, 250, 100, 2, 350);
                    foreach (var point in swPoints)
                    {
                        _dataPoints.Add(Math.Min((int)point, _maxCeiling));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 2000);
                }
                break;
            case "Square Wave (rounded)":
                {
                    var swPoints = PlotFunctionHelper.SquareWaveRoundedFunction(2, 250, 100, 2, 3, 350);
                    foreach (var point in swPoints)
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
                } break;
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
                } break;
            case "Quartic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInQuartic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                } break;
            case "Quintic":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        _dataPoints.Add(Math.Min((int)Extensions.EaseInQuintic(i), _maxCeiling));
                        if (_dataPoints[i - 1] == _maxCeiling) // If we've hit the ceiling then stop plotting.
                            break;
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                } break;
            case "Sinusoidal":
                {
                    double upShift = 250; // for graph offset since sine values will run negative
                    double amplitude = 145;
                    double frequency = 0.225; // higher value = more waves
                    double phaseShift = 0;
                    // Generate points for the sine wave
                    for (double t = 1; t <= 21; t += 0.1) // Adjust the step for more/less resolution
                    {
                        double y = amplitude * Math.Sin(Extensions.Tau * frequency * t + phaseShift);
                        _dataPoints.Add((int)(y + upShift));
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, (int)(upShift * 2));
                } break;
            case "Logarithmic (base 1)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        var newPoint = (int)Math.Floor(Math.Log(i, 1.0111));
                        _dataPoints.Add(newPoint);
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                } break;
            case "Logarithmic (base 2)":
                {
                    for (int i = 1; i < 101; i++)
                    {
                        var newPoint = (int)Math.Floor(Math.Log(i, 1.251));
                        _dataPoints.Add(newPoint);
                    }
                    DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
                } break;
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

            sender.DispatcherQueue.TryEnqueue(() => sender.IsEnabled = true);
        });
    }

    #region [Events]
    void PlotPageOnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loaded)
        {
            _loaded = true;
            cvsPlot.Margin = new Thickness(20, -10, 20, 50);

            // If we received data during constructor then plot it.
            if (_dataPoints.Count > 0)
                DrawCirclePlotDelayed(_dataPoints, cmbTypes, 0);
        }
    }

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
    /// TODO: Add opacity animation to the tooltip.
    /// </summary>
    void CircleOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawing)
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
}
