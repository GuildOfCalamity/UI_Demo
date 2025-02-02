using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Foundation;
using Windows.UI.StartScreen;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Effects;
using WinRT.Interop;

namespace UI_Demo;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    #region [Props]
    bool _loaded = false;
    bool _useSpinner = false;
    bool _useSurfaceBrush = false;
    static Brush? _lvl1;
    static Brush? _lvl2;
    static Brush? _lvl3;
    static Brush? _lvl4;
    static Brush? _lvl5;
    public event PropertyChangedEventHandler? PropertyChanged;
    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            NotifyPropertyChanged(nameof(IsBusy));
        }
    }
    public ObservableCollection<string> LogMessages { get; private set; } = new();
    #endregion

    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += MainPageOnLoaded;

        #region [MessageLevel Defaults]
        if (App.Current.Resources.TryGetValue("GradientDebugBrush", out object _))
            _lvl1 = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientDebugBrush"];
        else
            _lvl1 = new SolidColorBrush(Colors.Gray);

        if (App.Current.Resources.TryGetValue("GradientInfoBrush", out object _))
            _lvl2 = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientInfoBrush"];
        else
            _lvl2 = new SolidColorBrush(Colors.DodgerBlue);

        if (App.Current.Resources.TryGetValue("GradientImportantBrush", out object _))
            _lvl3 = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientImportantBrush"];
        else
            _lvl3 = new SolidColorBrush(Colors.Yellow);

        if (App.Current.Resources.TryGetValue("GradientWarningBrush", out object _))
            _lvl4 = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientWarningBrush"];
        else
            _lvl4 = new SolidColorBrush(Colors.Orange);

        if (App.Current.Resources.TryGetValue("GradientErrorBrush", out object _))
            _lvl5 = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["GradientErrorBrush"];
        else
            _lvl5 = new SolidColorBrush(Colors.Red);
        #endregion

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    public void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName)) { return; }
        // Confirm that we're on the UI thread in the event that DependencyProperty is changed under forked thread.
        DispatcherQueue.InvokeOnUI(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

     #region [Events]

    void ButtonOnClick(object sender, RoutedEventArgs e)
    {
        bool useTechnique1 = true;
        
        var _cts = new CancellationTokenSource();
        
        IsBusy = false;
        UpdateInfoBar("Starting task…", MessageLevel.Information);

        btnRun.IsEnabled = false;
        tbMessages.Text = "Running…";

        if (useTechnique1)
        {
            #region [*************** Technique #1 ***************]

            // Capture the UI context before forking.
            var syncContext = TaskScheduler.FromCurrentSynchronizationContext();

            var scanTask = Task.Run(async () =>
            {
                IsBusy = true;
                ToggleAnimation(IsBusy);
                await Task.Delay(3500);

            }, _cts.Token);

            /** 
             **   You wouldn't use both of these ContinueWith examples below, just select the one you're comfortable with.
             **   One demonstrates with synchronization context, and one demonstrates without synchronization context.
             **/
            #region [With Synchronization Context]
            // We're guaranteed the UI context when we come back, so any
            // FrameworkElement/UIElement/DependencyObject update can
            // be done directly via the control's properties.
            scanTask.ContinueWith(tsk =>
            {
                IsBusy = false;

                if (tsk.IsCanceled)
                    tbMessages.Text = "Process canceled.";
                else if (tsk.IsFaulted)
                {
                    if (tsk.Exception is AggregateException aex)
                    {
                        aex?.Flatten().Handle(ex =>
                        {
                            if (string.IsNullOrEmpty(ex?.Message))
                                tbMessages.Text = $"Task error: {aex.Message}";
                            else
                                tbMessages.Text = $"Task error: {ex?.Message}";
                            return true;
                        });
                    }
                    else
                        tbMessages.Text = $"Task error: {tsk.Exception?.GetBaseException().Message}";
                }
                else
                {
                    AddBlurCompositionElement(root, new Windows.UI.Color() { A = 255, R = 20, G = 20, B = 26 });
                    tbMessages.Text = "Process complete!";
                    _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog(), Content as FrameworkElement);
                    Task.Run(async () =>
                    {
                        await Task.Delay(3500);
                        RemoveBlurCompositionElement(root);
                    });
                }

                btnRun.IsEnabled = true;

                ToggleAnimation(IsBusy);

            }, syncContext);
            #endregion

            #region [Without Synchronization Context]
            // We're not guaranteed the UI context when we come back, so any
            // FrameworkElement/UIElement/DependencyObject update should be
            // done via the main DispatcherQueue or the control's Dispatcher.
            scanTask.ContinueWith(tsk =>
            {
                IsBusy = false;

                DispatcherQueue.InvokeOnUI(() => btnRun.IsEnabled = true);

                if (tsk.IsCanceled)
                    DispatcherQueue.InvokeOnUI(() => tbMessages.Text = "Process canceled.");
                else if (tsk.IsFaulted)
                {
                    if (tsk.Exception is AggregateException aex)
                    {
                        aex?.Flatten().Handle(ex =>
                        {
                            if (string.IsNullOrEmpty(ex?.Message))
                                DispatcherQueue.InvokeOnUI(() => tbMessages.Text = $"Task error: {aex.Message}");
                            else
                                DispatcherQueue.InvokeOnUI(() => tbMessages.Text = $"Task error: {ex?.Message}");
                            return true;
                        });
                    }
                    else
                        DispatcherQueue.InvokeOnUI(() => tbMessages.Text = $"Task error: {tsk.Exception?.GetBaseException().Message}");
                }
                else
                    DispatcherQueue.InvokeOnUI(() => tbMessages.Text = "Process complete!");

                ToggleAnimation(IsBusy);

            }, syncContext);
            #endregion

            #endregion [*************** Technique #1 ***************]
        }
        else
        {
            #region [*************** Technique #2 ***************]

            var dummyTask = SampleAsyncMethod(_cts.Token);

            /** Happens when successful **/
            dummyTask.ContinueWith(task =>
            {
                var list = task.Result; // Never access Task.Result unless the Task was successful.
                foreach (var thing in list) { LogMessages.Add(thing); }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());

            /** Happens when faulted **/
            dummyTask.ContinueWith(task =>
            {
                foreach (var ex in task.Exception!.Flatten().InnerExceptions) { tbMessages.Text = ex.Message; }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

            /** Happens when canceled **/
            dummyTask.ContinueWith(task =>
            {
                tbMessages.Text = "Dummy Process Canceled!";
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());

            /** Always happens **/
            dummyTask.ContinueWith(task =>
            {
                IsBusy = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            // Just a place-holder for this demo.
            async Task<List<string>> SampleAsyncMethod(CancellationToken cancelToken = new CancellationToken())
            {
                var list = new List<string>();
                for (int i = 1; i < 51; i++)
                {
                    list.Add($"Item {i}");
                    await Task.Delay(200);
                    cancelToken.ThrowIfCancellationRequested();
                }
                return list;
            }
            #endregion [*************** Technique #2 ***************]
        }
    }

    void ToggleAnimation(bool visible)
    {
        if (visible)
        {
            DispatcherQueue.InvokeOnUI(() =>
            {
                if (_useSpinner)
                {
                    imgSpin.Visibility = Visibility.Visible;
                    StoryboardSpin?.Begin();
                }
                else
                {
                    circles.IsRunning = visible;
                }
            });
        }
        else
        {
            DispatcherQueue.InvokeOnUI(() =>
            {
                if (_useSpinner)
                {
                    imgSpin.Visibility = Visibility.Collapsed;
                    StoryboardSpin?.Stop();
                }
                else
                {
                    circles.IsRunning = visible;
                }
            });
        }
    }

    void CheckBoxChanged(object sender, RoutedEventArgs e)
    {
        var cb = sender as CheckBox;
        if (this.Content != null) // use as "is loaded" check for the MainWindow
        {
            UpdateInfoBar($"Check box {((bool)cb.IsChecked ? "checked" : "unchecked")}.", (bool)cb.IsChecked ? MessageLevel.Important : MessageLevel.Warning);
            _useSurfaceBrush = (bool)cb.IsChecked;
            // Force the SpriteVisual to be recreated.
            if (_blurVisual != null)
            {
                _blurVisual.Dispose();
                _blurVisual = null;
            }
        }
    }

    void ToggleOnChanged(object sender, RoutedEventArgs e)
    {
        var tb = sender as ToggleButton;
        if (this.Content != null) // use as "is loaded" check for the MainWindow
        {
            UpdateInfoBar($"Toggle button {((bool)tb.IsChecked ? "checked" : "unchecked")}.", (bool)tb.IsChecked ? MessageLevel.Important : MessageLevel.Warning);
        }
    }

    void OnSwitchToggled(object sender, RoutedEventArgs e)
    {
        var ts = sender as ToggleSwitch;
        if (this.Content != null && ts != null) // use as "is loaded" check for the MainWindow
        {
            var ctrlPosition = ts.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0d, 0d));
            Debug.WriteLine($"[INFO] Relative control position: {ctrlPosition.X},{ctrlPosition.Y}");
            if (ts.IsOn)
            {
                AddBlurCompositionElement(root, new Windows.UI.Color() { A = 255, R = 20, G = 20, B = 32 }, useSurfaceBrush: _useSurfaceBrush, useImageForShadowMask: false);
            }
            else
            {
                RemoveBlurCompositionElement(root);
            }
            UpdateInfoBar($"Blur composition {(ts.IsOn ? "activated" : "deactivated")}.", ts.IsOn ? MessageLevel.Important : MessageLevel.Warning);
        }
    }

    async void SliderDaysChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var sld = sender as Slider;
        if (sld != null && this.Content != null) // use as "is loaded" check for the MainWindow
        {
            //UpdateInfoBar($"Slider changed to {(int)e.NewValue}");

            if (App.Profile != null)
                App.Profile.LastCount = (int)e.NewValue;

            if (((int)e.NewValue == sld.Minimum || (int)e.NewValue == sld.Maximum) && _loaded)
            {
                ContentDialogResult result = await DialogHelper.ShowAsync(new Dialogs.AboutDialog(), this.Content as FrameworkElement);
                if (result is ContentDialogResult.Primary) { UpdateInfoBar("User clicked 'OK'", MessageLevel.Important); }
                else if (result is ContentDialogResult.None) { UpdateInfoBar("User clicked 'Cancel'", MessageLevel.Warning); }
            }
        }
    }

    /// <summary>
    /// NOTE: You must set a background color on the grid for the mouse events to be triggered.
    /// You can use a transparent color e.g. #00111111 if you don't want a color to be visible.
    /// </summary>
    void HostGridOnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_loaded && !App.IsClosing)
        {
            try
            {
                var grid = sender as Grid;
                if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
                {
                    //var vector = e.GetCurrentPoint((UIElement)sender).Position.ToVector2();
                    var mousePoint = e.GetCurrentPoint(grid);
                    var position = mousePoint.Position;
                    if (mousePoint.Properties.IsRightButtonPressed)
                    {
                        var rbp = mousePoint.Properties.IsRightButtonPressed;
                        Debug.WriteLine($"[INFO] Mouse right-click detected at {position}");
                        FlyoutShowOptions options = new FlyoutShowOptions();
                        options.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;
                        options.Position = new Point(position.X / this.Content.XamlRoot.RasterizationScale, position.Y / this.Content.XamlRoot.RasterizationScale);
                        //TitlebarMenuFlyout.ShowAt(this.Content, options);
                        TitlebarMenuFlyout.ShowAt(grid, options);
                    }
                    else
                    {
                        Debug.WriteLine($"[INFO] Ignoring non right-click event at {position}");
                    }
                }
            }
            catch (Exception) { }
        }
    }

    void HostGridOnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_loaded && !App.IsClosing)
        {
            var localPoint = e.GetPosition(this);
            Debug.WriteLine($"[INFO] Tap detected at {localPoint}");
            if (Content is not null && Content.XamlRoot is not null)
            {
                if (TitlebarMenuFlyout.IsOpen)
                    TitlebarMenuFlyout.Hide();
            }
        }
    }

    void MainPageOnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_loaded && this.Content != null)
        {
            sldrDays.Value = App.Profile!.LastCount;
            if (App.ArgList.Count > 0)
            {
                UpdateInfoBar($"Received startup argument ⇒ {App.ArgList[0]}");
                if (App.ArgList[0].Contains("JumpList-OpenLog"))
                {
                    OpenDebugLog();
                }
            }
            else
                UpdateInfoBar($"App.ArgList.Count ⇒ {App.ArgList.Count}");
        }
        _loaded = true;
    }

    /// <summary>
    /// Communal event for <see cref="MenuFlyoutItem"/> clicks.
    /// The action performed will be based on the tag data.
    /// </summary>
    async void MenuFlyoutItemOnClick(object sender, RoutedEventArgs e)
    {
        var mfi = sender as MenuFlyoutItem;

        // Auto-hide if tag was passed like this ⇒ Tag="{x:Bind TitlebarMenuFlyout}"
        if (mfi is not null && mfi.Tag is not null && mfi.Tag is MenuFlyout mf)
        { 
            mf?.Hide(); 
            return; 
        }

        if (mfi is not null && mfi.Tag is not null)
        {
            var tag = $"{mfi.Tag}";
            if (!string.IsNullOrEmpty(tag) && tag.Equals("ActionClose", StringComparison.OrdinalIgnoreCase))
            {
                if (this.Content is not null && !App.IsClosing)
                {
                    AddBlurCompositionElement(root, new Windows.UI.Color() { A = 255, R = 20, G = 20, B = 26 });
                    ContentDialogResult result = await DialogHelper.ShowAsync(new Dialogs.CloseDialog(), Content as FrameworkElement);
                    if (result is ContentDialogResult.Primary)
                    {   // The closing event will be picked up in App.xaml.cs
                        (Application.Current as App)?.Exit();
                    }
                    else if (result is ContentDialogResult.None)
                    {
                        UpdateInfoBar($"User canceled the dialog.", MessageLevel.Information);
                        RemoveBlurCompositionElement(root);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(tag) && tag.Equals("ActionFirstRun", StringComparison.OrdinalIgnoreCase))
            {
                // Reset first run flag
                App.Profile!.FirstRun = true;
            }
            else
            {
                UpdateInfoBar($"No action has been defined for '{tag}'.", MessageLevel.Warning);
            }
        }
        else
        {
            UpdateInfoBar($"Tag data is empty for this MenuFlyoutItem.", MessageLevel.Error);
        }
    }

    /// <summary>
    /// Test for pinning app via <see cref="SecondaryTile"/>.
    /// </summary>
    async void ToolbarButtonOnClick(object sender, RoutedEventArgs e)
    {
        bool isPinnedSuccessfully = false;
        try
        {
            string tag = App.GetCurrentNamespace() ?? "WinUI Demo";

            SecondaryTile secondaryTile = new SecondaryTile($"WinUI_{tag}");
            secondaryTile.DisplayName = App.GetCurrentNamespace() ?? "WinUI Demo";
            secondaryTile.Arguments = $"SecondaryTile {tag}";

            secondaryTile.VisualElements.BackgroundColor = Colors.Transparent;
            secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png");
            secondaryTile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Square71x71Logo.scale-200.png");
            secondaryTile.VisualElements.Square44x44Logo = new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png");

            secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;

            var ip = (IntPtr)App.AppWin.Id.Value;
            if (ip != IntPtr.Zero)
            {
                InitializeWithWindow.Initialize(secondaryTile, (IntPtr)App.AppWin.Id.Value);
                isPinnedSuccessfully = await secondaryTile.RequestCreateAsync();
                UpdateInfoBar("App was pinned successfully", MessageLevel.Important);
            }
            else
            {
                UpdateInfoBar($"AppWin has no valid IntPtr", MessageLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(ex.Message))
                UpdateInfoBar($"Pin to start failed: {ex.Message}", MessageLevel.Error);
            else
                UpdateInfoBar($"Pin to start failed: {ex.StackTrace}", MessageLevel.Error);
        }
    }

    void TextBoxOnKeyUp(object sender, KeyRoutedEventArgs e) => UpdateInfoBar($"Key press: '{e.Key}'", MessageLevel.Information);
    #endregion

    void UpdateInfoBar(string msg, MessageLevel level = MessageLevel.Information)
    {
        if (App.IsClosing || infoBar == null)
            return;

        //DispatcherQueue.InvokeOnUI(() => { tbMessages.Text = msg; });

        _ = infoBar.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    {
                        tbMessages.Foreground = _lvl1;
                        infoBar.IsOpen = true;
                        infoBar.Message = msg;
                        infoBar.Severity = InfoBarSeverity.Informational;
                        break;
                    }
                case MessageLevel.Information:
                    {
                        tbMessages.Foreground = _lvl2;
                        infoBar.IsOpen = true;
                        infoBar.Message = msg;
                        infoBar.Severity = InfoBarSeverity.Informational;
                        break;
                    }
                case MessageLevel.Important:
                    {
                        tbMessages.Foreground = _lvl3;
                        infoBar.IsOpen = true;
                        infoBar.Message = msg;
                        infoBar.Severity = InfoBarSeverity.Success;
                        break;
                    }
                case MessageLevel.Warning:
                    {
                        tbMessages.Foreground = _lvl4;
                        infoBar.IsOpen = true;
                        infoBar.Message = msg;
                        infoBar.Severity = InfoBarSeverity.Warning;
                        break;
                    }
                case MessageLevel.Error:
                    {
                        tbMessages.Foreground = _lvl5;
                        infoBar.IsOpen = true;
                        infoBar.Message = msg;
                        infoBar.Severity = InfoBarSeverity.Error;
                        break;
                    }
            }
        });
    }

    /// <summary>
    /// Opens the current log file.
    /// </summary>
    public void OpenDebugLog()
    {
        string logPath = string.Empty;
        try
        {
            if (App.IsPackaged)
                logPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Debug.log");
            else
                logPath = System.IO.Path.Combine(AppContext.BaseDirectory, $"Debug.log");

            App.DebugLog($"Opening '{Path.GetFileName(logPath)}' with default viewer.");
            ThreadPool.QueueUserWorkItem((object? o) =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = logPath };
                Process.Start(startInfo);
            });
        }
        catch (Exception ex)
        {
            App.DebugLog($"OpenDebugLog: {ex.Message}");
        }
    }

    #region [Blur Effect Compositor]

    Compositor? _compositor;
    SpriteVisual? _blurVisual;
    ScalarKeyFrameAnimation? _showBlurAnimation;
    ScalarKeyFrameAnimation? _hideBlurAnimation;
    CompositionScopedBatch? _scopedBatch;

    /// <summary>
    /// A legit blur effect which will still allow the user to interact with the underlying controls.
    /// </summary>
    void AddBlurCompositionElement(FrameworkElement fe, Windows.UI.Color shadowColor, float shadowOpacity = 0.93F, bool useSurfaceBrush = false, bool useImageForShadowMask = false)
    {
        if (fe == null)
            return;

        CompositionSurfaceBrush? surfaceBrush = null;

        // Get the current compositor.
        if (_compositor == null)
            _compositor = ElementCompositionPreview.GetElementVisual(fe).Compositor;

        // If we're in the middle of an animation, cancel it now.
        if (_scopedBatch != null)
            CleanupScopeBatch();

        // If we've already created the sprite, just make it visible.
        if (_blurVisual != null)
        {
            _blurVisual.IsVisible = true;
            BlurInVisualAnimation();
            return;
        }

        if (useSurfaceBrush)
        {   // Create surface brush and load image.
            surfaceBrush = _compositor.CreateSurfaceBrush();
            // Use an image as the shadow.
            surfaceBrush.Surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/BlurPane.png"));
            surfaceBrush.Stretch = CompositionStretch.UniformToFill;
            surfaceBrush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.MagNearestMinLinearMipLinear;
            //surfaceBrush.Scale = new Vector2 { X = 1.5f, Y = 1.5f };
        }

        // Create the destination sprite, sized to cover the entire page.
        _blurVisual = _compositor.CreateSpriteVisual();
        if (fe.ActualSize != Vector2.Zero)
            _blurVisual.Size = new Vector2((float)fe.ActualWidth, (float)fe.ActualHeight);
        else
            _blurVisual.Size = new Vector2((float)App.m_width, (float)App.m_height);

        if (surfaceBrush != null)
            _blurVisual.Brush = surfaceBrush;

        // Create drop shadow...
        DropShadow shadow = _compositor.CreateDropShadow();
        shadow.Opacity = shadowOpacity;
        shadow.Color = shadowColor;
        shadow.BlurRadius = 100F;
        shadow.Offset = new System.Numerics.Vector3(0, 0, -1);
        if (useImageForShadowMask)
        {   // Specify mask policy for shadow.
            shadow.SourcePolicy = CompositionDropShadowSourcePolicy.InheritFromVisualContent;
        }
        // Associate shadow with visual.
        _blurVisual.Shadow = shadow;

        // Start out with the destination layer visible.
        _blurVisual.IsVisible = true;

        // Apply the visual element.
        ElementCompositionPreview.SetElementChildVisual(fe, _blurVisual);

        BlurInVisualAnimation();
    }

    /// <summary>
    /// Dispose of any sprite and unparent it from the FrameworkElement.
    /// If the <see cref="SpriteVisual"/> has already been created, it will be hidden.
    /// </summary>
    /// <param name="fe"><see cref="FrameworkElement"/></param>
    void RemoveBlurCompositionElement(FrameworkElement fe)
    {
        if (fe == null)
            return;

        if (_blurVisual != null)
        {
            //_blurVisual.IsVisible = false;
            BlurOutVisualAnimation();
            return;
        }
        ElementCompositionPreview.SetElementChildVisual(fe, null);
    }

    /// <summary>
    /// Animate the <see cref="SpriteVisual"/> to fade in using <see cref="ScalarKeyFrameAnimation"/>.
    /// </summary>
    void BlurInVisualAnimation()
    {
        if (_blurVisual == null || _compositor == null)
            return;

        // Add an easing function: this will ramp quickly at first and then slow down. The default easing on animations is always linear.
        var ease = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.4f), new Vector2(0.9f, 0.25f));

        // Animate from transparent to fully opaque or translucent (depends on brush and image)
        if (_showBlurAnimation == null)
        {
            _showBlurAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _showBlurAnimation.InsertKeyFrame(0f, 0f); //showAnimation.InsertKeyFrame(0f, 0f, ease);
            _showBlurAnimation.InsertKeyFrame(1f, 1f); //showAnimation.InsertKeyFrame(1f, 1f, ease);
            _showBlurAnimation.Duration = TimeSpan.FromMilliseconds(1200);
        }
        _blurVisual.StartAnimation("Opacity", _showBlurAnimation);
    }

    /// <summary>
    /// Animate the <see cref="SpriteVisual"/> to fade out using <see cref="ScalarKeyFrameAnimation"/>.
    /// </summary>
    void BlurOutVisualAnimation()
    {
        if (_blurVisual == null || _compositor == null)
            return;

        // Start a scoped batch so we can register to completion event and hide the destination layer.
        _scopedBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

        // Start the hide animation to fade out the destination effect.
        if (_hideBlurAnimation == null)
        {
            _hideBlurAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _hideBlurAnimation.InsertKeyFrame(0f, 1f);
            _hideBlurAnimation.InsertKeyFrame(1f, 0f);
            _hideBlurAnimation.Duration = TimeSpan.FromMilliseconds(1200);
        }
        _blurVisual.StartAnimation("Opacity", _hideBlurAnimation);

        // Scoped batch completed event.
        _scopedBatch.Completed += ScopeBatchCompleted;
        _scopedBatch.End();
    }

    /// <summary>
    /// Once the <see cref="ScalarKeyFrameAnimation"/> completes, hide the <see cref="SpriteVisual"/>.
    /// </summary>
    void ScopeBatchCompleted(object sender, CompositionBatchCompletedEventArgs args)
    {
        if (_blurVisual == null)
            return;

        Debug.WriteLine($"[INFO] {nameof(_scopedBatch)} completed event");

        // Scope batch completion event has fired, hide the destination sprite and cleanup the batch.
        try { _blurVisual.IsVisible = false; }
        catch (ObjectDisposedException) { Debug.WriteLine($"[WARNING] {nameof(_blurVisual)} is already disposed."); }

        CleanupScopeBatch();
    }

    /// <summary>
    /// Dispose of the <see cref="CompositionScopedBatch"/> and unhook the event.
    /// </summary>
    void CleanupScopeBatch()
    {
        if (_scopedBatch != null)
        {
            _scopedBatch.Completed -= ScopeBatchCompleted;
            _scopedBatch.Dispose();
            _scopedBatch = null;
        }
    }
    #endregion
}