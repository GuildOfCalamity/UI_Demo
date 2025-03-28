using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UI_Demo;

/// <summary>
///   Represents the control that redistributes space between columns or rows of a Grid control.
/// </summary>
/// <remarks>
///   Reworked from https://github.com/CommunityToolkit/WindowsCommunityToolkit/tree/winui/CommunityToolkit.WinUI.UI.Controls.Layout/GridSplitter
/// </remarks>
public partial class GridSplitter
{
    static bool IsStarColumn(ColumnDefinition definition)
    {
        return ((GridLength)definition.GetValue(ColumnDefinition.WidthProperty)).IsStar;
    }

    static bool IsStarRow(RowDefinition definition)
    {
        return ((GridLength)definition.GetValue(RowDefinition.HeightProperty)).IsStar;
    }

    bool SetColumnWidth(ColumnDefinition columnDefinition, double horizontalChange, GridUnitType unitType)
    {
        var newWidth = columnDefinition.ActualWidth + horizontalChange;

        var minWidth = columnDefinition.MinWidth;
        if (!double.IsNaN(minWidth) && newWidth < minWidth)
            newWidth = minWidth;

        var maxWidth = columnDefinition.MaxWidth;
        if (!double.IsNaN(maxWidth) && newWidth > maxWidth)
            newWidth = maxWidth;

        if (newWidth > ActualWidth)
        {
            columnDefinition.Width = new GridLength(newWidth, unitType);
            return true;
        }
        return false;
    }

    bool IsValidColumnWidth(ColumnDefinition columnDefinition, double horizontalChange)
    {
        var newWidth = columnDefinition.ActualWidth + horizontalChange;

        var minWidth = columnDefinition.MinWidth;
        if (!double.IsNaN(minWidth) && newWidth < minWidth)
            return false;

        var maxWidth = columnDefinition.MaxWidth;
        if (!double.IsNaN(maxWidth) && newWidth > maxWidth)
            return false;

        if (newWidth <= ActualWidth)
            return false;

        return true;
    }

    bool SetRowHeight(RowDefinition rowDefinition, double verticalChange, GridUnitType unitType)
    {
        var newHeight = rowDefinition.ActualHeight + verticalChange;

        var minHeight = rowDefinition.MinHeight;
        if (!double.IsNaN(minHeight) && newHeight < minHeight)
            newHeight = minHeight;

        var maxWidth = rowDefinition.MaxHeight;
        if (!double.IsNaN(maxWidth) && newHeight > maxWidth)
            newHeight = maxWidth;

        if (newHeight > ActualHeight)
        {
            rowDefinition.Height = new GridLength(newHeight, unitType);
            return true;
        }
        return false;
    }

    bool IsValidRowHeight(RowDefinition rowDefinition, double verticalChange)
    {
        var newHeight = rowDefinition.ActualHeight + verticalChange;

        var minHeight = rowDefinition.MinHeight;
        if (!double.IsNaN(minHeight) && newHeight < minHeight)
            return false;

        var maxHeight = rowDefinition.MaxHeight;
        if (!double.IsNaN(maxHeight) && newHeight > maxHeight)
            return false;

        if (newHeight <= ActualHeight)
            return false;

        return true;
    }

    // Return the targeted Column based on the resize behavior
    int GetTargetedColumn()
    {
        var currentIndex = Grid.GetColumn(TargetControl);
        return GetTargetIndex(currentIndex);
    }

    // Return the sibling Row based on the resize behavior
    int GetTargetedRow()
    {
        var currentIndex = Grid.GetRow(TargetControl);
        return GetTargetIndex(currentIndex);
    }

    // Return the sibling Column based on the resize behavior
    int GetSiblingColumn()
    {
        var currentIndex = Grid.GetColumn(TargetControl);
        return GetSiblingIndex(currentIndex);
    }

    // Return the sibling Row based on the resize behavior
    int GetSiblingRow()
    {
        var currentIndex = Grid.GetRow(TargetControl);
        return GetSiblingIndex(currentIndex);
    }

    // Gets index based on resize behavior for first targeted row/column
    int GetTargetIndex(int currentIndex)
    {
        switch (_resizeBehavior)
        {
            case GridResizeBehavior.CurrentAndNext:
                return currentIndex;
            case GridResizeBehavior.PreviousAndNext:
                return currentIndex - 1;
            case GridResizeBehavior.PreviousAndCurrent:
                return currentIndex - 1;
            default:
                return -1;
        }
    }

    // Gets index based on resize behavior for second targeted row/column
    int GetSiblingIndex(int currentIndex)
    {
        switch (_resizeBehavior)
        {
            case GridResizeBehavior.CurrentAndNext:
                return currentIndex + 1;
            case GridResizeBehavior.PreviousAndNext:
                return currentIndex + 1;
            case GridResizeBehavior.PreviousAndCurrent:
                return currentIndex;
            default:
                return -1;
        }
    }

    // Checks the control alignment and Width/Height to detect the control resize direction columns/rows
    GridResizeDirection GetResizeDirection()
    {
        GridResizeDirection direction = ResizeDirection;

        if (direction == GridResizeDirection.Auto)
        {
            // When HorizontalAlignment is Left, Right or Center, resize Columns
            if (HorizontalAlignment != HorizontalAlignment.Stretch)
                direction = GridResizeDirection.Columns;
            // When VerticalAlignment is Top, Bottom or Center, resize Rows
            else if (VerticalAlignment != VerticalAlignment.Stretch)
                direction = GridResizeDirection.Rows;
            // Check Width vs Height
            else if (ActualWidth <= ActualHeight)
                direction = GridResizeDirection.Columns;
            else
                direction = GridResizeDirection.Rows;
        }

        return direction;
    }

    // Get the resize behavior (Which columns/rows should be resized) based on alignment and Direction
    GridResizeBehavior GetResizeBehavior()
    {
        GridResizeBehavior resizeBehavior = ResizeBehavior;

        if (resizeBehavior == GridResizeBehavior.BasedOnAlignment)
        {
            if (_resizeDirection == GridResizeDirection.Columns)
            {
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left: resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                        break;
                    case HorizontalAlignment.Right: resizeBehavior = GridResizeBehavior.CurrentAndNext;
                        break;
                    default: resizeBehavior = GridResizeBehavior.PreviousAndNext;
                        break;
                }
            }
            // resize direction is vertical
            else
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Top: resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                        break;
                    case VerticalAlignment.Bottom: resizeBehavior = GridResizeBehavior.CurrentAndNext;
                        break;
                    default: resizeBehavior = GridResizeBehavior.PreviousAndNext;
                        break;
                }
            }
        }
        return resizeBehavior;
    }
}