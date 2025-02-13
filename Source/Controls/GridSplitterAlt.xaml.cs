using System;
using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Foundation;

namespace UI_Demo;

public sealed partial class GridSplitterAlt : UserControl
{
    bool _initialized = false;
    bool _isDragging = false;
    bool _isPressed = false;
    Point _startPoint;
    ColumnDefinition? _column;
    RowDefinition? _row;

    internal InputCursor? PreviousCursor { get; set; }
    internal static readonly InputCursor ColumnsSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    internal static readonly InputCursor RowSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
    internal static readonly InputCursor SplitterCursorHover = InputSystemCursor.Create(InputSystemCursorShape.Hand);


    public GridSplitterAlt()
    {
        this.InitializeComponent();
        AutomationProperties.SetName(this, "GridSplitterAlt");

        this.Loaded += OnControlLoaded;
        this.PointerMoved += OnPointerMoved;
        this.PointerPressed += OnPointerPressed;
        this.PointerReleased += OnPointerReleased;
        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;
        this.PointerReleased += OnPointerReleased;
        this.PointerCaptureLost += OnPointerReleased;
        this.ManipulationStarted += OnManipulationStarted;
        this.ManipulationCompleted += OnManipulationCompleted;

        ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _initialized = true;
    }

    void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] Loaded {sender.GetType().Name} of base type {sender.GetType().BaseType?.Name}");
    }

    void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPressed = true;
        _startPoint = e.GetCurrentPoint(this).Position;

        // Get parent Grid
        var parentGrid = this.Parent as Grid;
        if (parentGrid == null)
        {
            Debug.WriteLine("[WARNING] GridSplitter must be a child of a Grid control.");
            return;
        }

        // Determine whether we're in a row or column
        int columnIndex = Grid.GetColumn(this);
        int rowIndex = Grid.GetRow(this);

        if (columnIndex > 0)
            _column = parentGrid.ColumnDefinitions[columnIndex - 1];

        if (rowIndex > 0)
            _row = parentGrid.RowDefinitions[rowIndex - 1];

        Debug.WriteLine($"[INFO] GridSplitter capturing pointer.");
        CapturePointer(e.Pointer);
    }

    void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPressed = false;
        Debug.WriteLine($"[INFO] GridSplitter releasing pointer.");
        ReleasePointerCapture(e.Pointer);
    }

    void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) { return; }

        Point currentPoint = e.GetCurrentPoint(this).Position;
        double deltaX = currentPoint.X - _startPoint.X;
        double deltaY = currentPoint.Y - _startPoint.Y;

        Debug.WriteLine($"[INFO] GridSplitter pointer moved: {currentPoint}.  DeltaX={deltaX}  DeltaY={deltaY}");

        // Adjust column width
        if (_column != null && _column.Width.IsStar)
        {
            double newWidth = Math.Max(20, _column.ActualWidth + deltaX);
            Debug.WriteLine($"[INFO] Adjusting column width to {newWidth}.");
            _column.Width = new GridLength(newWidth, GridUnitType.Star);
        }
        else if (_column != null)
        {
            Debug.WriteLine($"[WARNING] Did not detect Column.Width.IsStar");
        }

        // Adjust row height
        if (_row != null && _row.Height.IsStar)
        {
            double newHeight = Math.Max(20, _row.ActualHeight + deltaY);
            Debug.WriteLine($"[INFO] Adjusting row height to {newHeight}.");
            _row.Height = new GridLength(newHeight, GridUnitType.Star);
        }
        else if (_row != null)
        {
            Debug.WriteLine($"[WARNING] Did not detect Row.Height.IsStar");
        }

        _startPoint = currentPoint;
    }

    void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (App.Current.Resources.TryGetValue("GradientSplitterHoverBrush", out object _))
        {
            Grabber.Fill = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientSplitterHoverBrush"];
        }
        ProtectedCursor = SplitterCursorHover;
    }

    void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (App.Current.Resources.TryGetValue("GradientSplitterBrush", out object _))
        {
            Grabber.Fill = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientSplitterBrush"];
        }
    }

    void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        _isDragging = true;
        PreviousCursor = ProtectedCursor;
        Debug.WriteLine($"[INFO] Manipulation started:  IsDragging={_isDragging}  IsPressed={_isPressed}");
    }

    void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        _isDragging = false;
        ProtectedCursor = PreviousCursor;
        Debug.WriteLine($"[INFO] Manipulation completed:  IsDragging={_isDragging}  IsPressed={_isPressed}");
    }

}