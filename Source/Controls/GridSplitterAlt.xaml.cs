using System;
using System.Diagnostics;
using System.Numerics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Foundation;

namespace UI_Demo;

public sealed partial class GridSplitterAlt : UserControl
{
    bool _initialized = false;
    bool _isDragging = false;
    Point _startPoint;
    ColumnDefinition? _column;
    RowDefinition? _row;

    public GridSplitterAlt()
    {
        this.InitializeComponent();
        //this.PointerReleased += OnPointerReleased;
        this.PointerCaptureLost += OnPointerReleased; // Ensure pointer stays captured
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
        _isDragging = true;

        //_startPoint = e.GetCurrentPoint(Window.Current.Content).Position;
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

    void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        //Point currentPoint = e.GetCurrentPoint(Window.Current.Content).Position;
        Point currentPoint = e.GetCurrentPoint(this).Position;

        double deltaX = currentPoint.X - _startPoint.X;
        double deltaY = currentPoint.Y - _startPoint.Y;

        // Adjust column width
        if (_column != null && _column.Width.IsStar)
        {
            double newWidth = Math.Max(20, _column.ActualWidth + deltaX);
            _column.Width = new GridLength(newWidth, GridUnitType.Star);
            Debug.WriteLine($"[INFO] Adjusting column width to {newWidth}.");
        }

        // Adjust row height
        if (_row != null && _row.Height.IsStar)
        {
            double newHeight = Math.Max(20, _row.ActualHeight + deltaY);
            _row.Height = new GridLength(newHeight, GridUnitType.Star);
            Debug.WriteLine($"[INFO] Adjusting row height to {newHeight}.");
        }

        _startPoint = currentPoint;
    }

    void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        Debug.WriteLine($"[INFO] GridSplitter releasing pointer.");
        ReleasePointerCapture(e.Pointer);
    }
}