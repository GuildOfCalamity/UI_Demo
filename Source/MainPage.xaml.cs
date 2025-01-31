using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Foundation;
using Windows.UI.StartScreen;
using WinRT.Interop;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;

namespace UI_Demo;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    #region [Props]
    static bool _loaded = false;
    static bool _useSpinner = false;
    static Brush? _lvl1;
    static Brush? _lvl2;
    static Brush? _lvl3;
    static Brush? _lvl4;
    static Brush? _lvl5;
    Border? _blurBorder;
    AcrylicBrush? _blurBrush;
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

        #region [Blur Testing]
        _blurBrush = new AcrylicBrush
        {
            TintOpacity = 0.2,
            TintLuminosityOpacity = 0.1,
            TintColor = Windows.UI.Color.FromArgb(255, 49, 122, 215),
            FallbackColor = Windows.UI.Color.FromArgb(255, 49, 122, 215)
        };

        _blurBorder = new Border
        {
            // Adjust the top margin to account for the TitleBar height,
            // or you could wire up this feature in the MainWindow.
            Margin = new Thickness(0, -31, 0,0),
            Background = _blurBrush,
            Opacity = 0.8,
            CornerRadius = new CornerRadius(0)
        };
        #endregion
    }

    public void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName)) { return; }
        // Confirm that we're on the UI thread in the event that DependencyProperty is changed under forked thread.
        DispatcherQueue.InvokeOnUI(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
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
                    SetBlur(true, true);
                    tbMessages.Text = "Process complete!";
                    _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog(), Content as FrameworkElement);
                    Task.Run(async () =>
                    {
                        await Task.Delay(3500);
                        SetBlur(false, true);
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
        if (this.Content != null) // use as "is loaded" check for the MainWindow
        {
            UpdateInfoBar($"Toggle switch {(ts.IsOn ? "activated" : "deactivated")}.", ts.IsOn ? MessageLevel.Important : MessageLevel.Warning);
            SetBlur(ts.IsOn, false);
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
                    SetBlur(true, true);
                    ContentDialogResult result = await DialogHelper.ShowAsync(new Dialogs.CloseDialog(), Content as FrameworkElement);
                    if (result is ContentDialogResult.Primary)
                    {   // The closing event will be picked up in App.xaml.cs
                        (Application.Current as App)?.Exit();
                    }
                    else if (result is ContentDialogResult.None)
                    {
                        UpdateInfoBar($"User canceled the dialog.", MessageLevel.Information);
                        SetBlur(false, true);
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

    /// <summary>
    /// A test for changing the order of UIElements to simulate a blur effect.
    /// </summary>
    /// <param name="enable">true to use the <see cref="Microsoft.UI.Xaml.Media.AcrylicBrush"/>, false otherwise</param>
    /// <param name="isOverlay">true inserts the border at top of z-order, false to remove border</param>
    public void SetBlur(bool enable, bool isOverlay = false)
    {
        DispatcherQueue.InvokeOnUI(() => 
        {
            if (!isOverlay)
            {
                if (enable)
                {
                    root.Background = _blurBrush;
                    //var fadeInAnimation = new DoubleAnimation
                    //{
                    //    From = 0,
                    //    To = 1,
                    //    Duration = new Duration(TimeSpan.FromMilliseconds(2000)),
                    //    EasingFunction = new QuadraticEase()
                    //};
                    //var storyboard = new Storyboard();
                    //Storyboard.SetTarget(fadeInAnimation, root);
                    //Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
                    //storyboard.Children.Add(fadeInAnimation);
                    //storyboard.Begin();
                }
                else
                {
                    root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 10, 10, 10));
                    //var fadeInAnimation = new DoubleAnimation
                    //{
                    //    From = 1,
                    //    To = 0,
                    //    Duration = new Duration(TimeSpan.FromMilliseconds(2000)),
                    //    EasingFunction = new QuadraticEase()
                    //};
                    //var storyboard = new Storyboard();
                    //Storyboard.SetTarget(fadeInAnimation, root);
                    //Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
                    //storyboard.Children.Add(fadeInAnimation);
                    //storyboard.Begin();
                }

            }
            else
            {
                if (_blurBorder == null)
                    return;
                
                //var container = (Page)root.Parent;
                
                var visual = ElementCompositionPreview.GetElementVisual(_blurBorder);
                if (visual != null)
                    visual.BorderMode = Microsoft.UI.Composition.CompositionBorderMode.Soft;
                
                if (enable)
                    root.Children.Insert(root.Children.Count, _blurBorder); // Move to top
                else
                    root.Children.Remove(_blurBorder);
            }
        });
    }

}
