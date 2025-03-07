using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace UI_Demo;

/** [EXAMPLE STYLER]
<SolidColorBrush x:Key="SystemControlSplitterPointerOver" Color="{ThemeResource SystemBaseLowColor}" />
<SolidColorBrush x:Key="SystemControlSplitterPressed" Color="{ThemeResource SystemBaseHighColor}" />
<Style TargetType="local:GridSplitter">
    <Setter Property="IsTabStop" Value="True" />
    <Setter Property="UseSystemFocusVisuals" Value="True" />
    <Setter Property="HorizontalAlignment" Value="Stretch" />
    <Setter Property="VerticalAlignment" Value="Stretch" />
    <Setter Property="IsFocusEngagementEnabled" Value="True" />
    <Setter Property="MinWidth" Value="16" />
    <Setter Property="MinHeight" Value="16" />
    <Setter Property="Background" Value="{ThemeResource SystemControlHighlightChromeHighBrush}" />
    <Setter Property="GripperForeground" Value="{ThemeResource SystemControlForegroundAltHighBrush}" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="local:GridSplitter">
                <Grid
                    x:Name="RootGrid"
                    Background="{TemplateBinding Background}"
                    CornerRadius="{TemplateBinding CornerRadius}">
                    <ContentPresenter
                        HorizontalContentAlignment="Stretch"
                        VerticalContentAlignment="Stretch"
                        Content="{TemplateBinding Element}" />
                    <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="GridSplitterStates">
                            <VisualState x:Name="Normal" />
                            <VisualState x:Name="PointerOver">
                                <VisualState.Setters>
                                    <Setter Target="RootGrid.Background" Value="{ThemeResource SystemControlSplitterPointerOver}" />
                                </VisualState.Setters>
                            </VisualState>
                            <VisualState x:Name="Pressed">
                                <VisualState.Setters>
                                    <Setter Target="RootGrid.Background" Value="{ThemeResource SystemControlSplitterPressed}" />
                                </VisualState.Setters>
                            </VisualState>
                        </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
**/

/// <summary>
///   Represents the control that redistributes space between columns or rows of a Grid control.
/// </summary>
/// <remarks>
///   NOTE: Make sure you have added the appropriate styler to your "App.xaml" file before using the <see cref="GridSplitter"/>.
///   Reworked from https://github.com/CommunityToolkit/WindowsCommunityToolkit/tree/winui/CommunityToolkit.WinUI.UI.Controls.Layout/GridSplitter
/// </remarks>
public partial class GridSplitter : Control
{
    internal const int GripperCustomCursorDefaultResource = -1;
    internal static readonly InputCursor ColumnsSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    internal static readonly InputCursor RowSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
    internal static readonly InputCursor SplitterCursorHover = InputSystemCursor.Create(InputSystemCursorShape.Hand);

    internal InputCursor? PreviousCursor { get; set; }

    private GridResizeDirection _resizeDirection;
    private GridResizeBehavior _resizeBehavior;
    private GripperHoverWrapper? _hoverWrapper;
    private TextBlock? _gripperDisplay;

    private bool _pressed = false;
    private bool _dragging = false;
    private bool _pointerEntered = false;

    #region [Props]
    /// <summary>
    /// Gets the target parent grid from level
    /// </summary>
    private FrameworkElement TargetControl
    {
        get
        {
            if (ParentLevel == 0)
                return this;

            var parent = Parent;
            for (int i = 2; i < ParentLevel; i++)
            {
                var frameworkElement = parent as FrameworkElement;
                if (frameworkElement != null)
                    parent = frameworkElement.Parent;
            }

            return parent as FrameworkElement;
        }
    }

    /// <summary>
    /// Gets GridSplitter Container Grid
    /// </summary>
    private Grid Resizable => TargetControl?.Parent as Grid;

    /// <summary>
    /// Gets the current Column definition of the parent Grid
    /// </summary>
    private ColumnDefinition CurrentColumn
    {
        get
        {
            if (Resizable == null)
                return null;

            var gridSplitterTargetedColumnIndex = GetTargetedColumn();

            if ((gridSplitterTargetedColumnIndex >= 0) && (gridSplitterTargetedColumnIndex < Resizable.ColumnDefinitions.Count))
                return Resizable.ColumnDefinitions[gridSplitterTargetedColumnIndex];

            return null;
        }
    }

    /// <summary>
    /// Gets the Sibling Column definition of the parent Grid
    /// </summary>
    private ColumnDefinition SiblingColumn
    {
        get
        {
            if (Resizable == null)
                return null;

            var gridSplitterSiblingColumnIndex = GetSiblingColumn();

            if ((gridSplitterSiblingColumnIndex >= 0) && (gridSplitterSiblingColumnIndex < Resizable.ColumnDefinitions.Count))
                return Resizable.ColumnDefinitions[gridSplitterSiblingColumnIndex];

            return null;
        }
    }

    /// <summary>
    /// Gets the current Row definition of the parent Grid
    /// </summary>
    private RowDefinition CurrentRow
    {
        get
        {
            if (Resizable == null)
                return null;

            var gridSplitterTargetedRowIndex = GetTargetedRow();

            if ((gridSplitterTargetedRowIndex >= 0) && (gridSplitterTargetedRowIndex < Resizable.RowDefinitions.Count))
                return Resizable.RowDefinitions[gridSplitterTargetedRowIndex];

            return null;
        }
    }

    /// <summary>
    /// Gets the Sibling Row definition of the parent Grid
    /// </summary>
    private RowDefinition SiblingRow
    {
        get
        {
            if (Resizable == null)
                return null;

            var gridSplitterSiblingRowIndex = GetSiblingRow();

            if ((gridSplitterSiblingRowIndex >= 0) && (gridSplitterSiblingRowIndex < Resizable.RowDefinitions.Count))
                return Resizable.RowDefinitions[gridSplitterSiblingRowIndex];

            return null;
        }
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GridSplitter"/> class.
    /// </summary>
    public GridSplitter()
    {
        DefaultStyleKey = typeof(GridSplitter);
        //Loaded += GridSplitter_Loaded; // doubled subscription?
        AutomationProperties.SetName(this, "GridSplitter");
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Unhook registered events
        Loaded -= GridSplitter_Loaded;
        PointerEntered -= GridSplitter_PointerEntered;
        PointerExited -= GridSplitter_PointerExited;
        PointerPressed -= GridSplitter_PointerPressed;
        PointerReleased -= GridSplitter_PointerReleased;
        ManipulationStarted -= GridSplitter_ManipulationStarted;
        ManipulationCompleted -= GridSplitter_ManipulationCompleted;

        _hoverWrapper?.UnhookEvents();

        // Register Events
        Loaded += GridSplitter_Loaded;
        PointerEntered += GridSplitter_PointerEntered;
        PointerExited += GridSplitter_PointerExited;
        PointerPressed += GridSplitter_PointerPressed;
        PointerReleased += GridSplitter_PointerReleased;
        ManipulationStarted += GridSplitter_ManipulationStarted;
        ManipulationCompleted += GridSplitter_ManipulationCompleted;

        _hoverWrapper?.UpdateHoverElement(Element);

        ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
    }

    #region [VisualStateManager]
    void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _pressed = false;
        VisualStateManager.GoToState(this, _pointerEntered ? "PointerOver" : "Normal", true);
    }

    void GridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _pressed = true;
        VisualStateManager.GoToState(this, "Pressed", true);
    }

    void GridSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _pointerEntered = false;
        if (!_pressed && !_dragging)
            VisualStateManager.GoToState(this, "Normal", true);
    }

    void GridSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _pointerEntered = true;
        if (!_pressed && !_dragging)
        {
            ProtectedCursor = SplitterCursorHover;
            VisualStateManager.GoToState(this, "PointerOver", true);
        }
    }

    void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        _dragging = false;
        _pressed = false;
        VisualStateManager.GoToState(this, _pointerEntered ? "PointerOver" : "Normal", true);
    }

    void GridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        _dragging = true;
        VisualStateManager.GoToState(this, "Pressed", true);
    }
    #endregion
}