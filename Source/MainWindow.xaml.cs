using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.UI;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation.Diagnostics;
using Windows.UI.StartScreen;

using WinRT.Interop;

namespace UI_Demo;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    #region [Props]
    static bool _firstVisible = false;
    static bool _useSpinner = false;
    static Brush? _lvl1;
    static Brush? _lvl2;
    static Brush? _lvl3;
    static Brush? _lvl4;
    static Brush? _lvl5;
    ContentCoordinateConverter _coordinateConverter;
    OverlappedPresenter? _overlapPresenter;

    public event PropertyChangedEventHandler? PropertyChanged;
    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
        }
    }
    public ObservableCollection<string> LogMessages { get; private set; } = new();
    #endregion

    public MainWindow()
    {
        this.InitializeComponent();
        this.VisibilityChanged += MainWindowOnVisibilityChanged;
        //this.SizeChanged += MainWindowOnSizeChanged; // We're already using this in CreateGradientBackdrop().
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
    }

    /// <summary>
    /// An impromptu OnLoaded event. 
    /// It would be better to read from the AppWin.Changed event, but this works fine.
    /// </summary>
    void MainWindowOnVisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        if (!_firstVisible && this.Content != null)
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
        _firstVisible = true;
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

    #region [Events]

    void ButtonOnClick(object sender, RoutedEventArgs e)
    {
        UpdateInfoBar("Button Click Event Detected", MessageLevel.Information);
        IsBusy = false;
        var _cts = new CancellationTokenSource();
        
        btnRun.IsEnabled = false;
        tbMessages.Text = "Running...";

        #region [*************** Technique #1 ***************]

        // Capture the UI context before forking.
        var syncContext = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();

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
                {
                    tbMessages.Text = $"Task error: {tsk.Exception?.GetBaseException().Message}";
                }
            }
            else
                tbMessages.Text = "Process complete!";

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
                {
                    DispatcherQueue.InvokeOnUI(() => tbMessages.Text = $"Task error: {tsk.Exception?.GetBaseException().Message}");
                }
            }
            else
                DispatcherQueue.InvokeOnUI(() => tbMessages.Text = "Process complete!");

            ToggleAnimation(IsBusy);

        }, syncContext);
        #endregion

        #endregion [*************** Technique #1 ***************]

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
            for (int i = 0; i < 100; i++)
            {
                list.Add($"Item {i}");
                await Task.Delay(300, cancelToken);
                cancelToken.ThrowIfCancellationRequested();
            }
            return list;
        }
        #endregion [*************** Technique #2 ***************]
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
            UpdateInfoBar($"Check box {((bool)cb.IsChecked ? "checked" : "unchecked")}.", (bool)cb.IsChecked ? MessageLevel.Important : MessageLevel.Warning);
    }

    void ToggleOnChanged(object sender, RoutedEventArgs e)
    {
        var tb = sender as ToggleButton;
        if (this.Content != null) // use as "is loaded" check for the MainWindow
            UpdateInfoBar($"Toggle button {((bool)tb.IsChecked ? "checked" : "unchecked")}.", (bool)tb.IsChecked ? MessageLevel.Important : MessageLevel.Warning);
    }

    void OnSwitchToggled(object sender, RoutedEventArgs e)
    {
        var ts = sender as ToggleSwitch;
        if (this.Content != null) // use as "is loaded" check for the MainWindow
            UpdateInfoBar($"Toggle switch {(ts.IsOn ? "activated" : "deactivated")}.", ts.IsOn ? MessageLevel.Important : MessageLevel.Warning);
    }

    async void SliderDaysChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var sld = sender as Slider;
        if (sld != null && this.Content != null) // use as "is loaded" check for the MainWindow
        {
            //UpdateInfoBar($"Slider changed to {(int)e.NewValue}");
            
            if (App.Profile != null)
                App.Profile.LastCount = (int)e.NewValue;

            if (((int)e.NewValue == sld.Minimum || (int)e.NewValue == sld.Maximum) && _firstVisible)
            {
                ContentDialogResult result = await DialogHelper.ShowAsync(new Dialogs.AboutDialog(), this.Content as FrameworkElement);
                if (result is ContentDialogResult.Primary) { UpdateInfoBar("User clicked 'OK'", MessageLevel.Important); }
                else if (result is ContentDialogResult.None) { UpdateInfoBar("User clicked 'Cancel'", MessageLevel.Warning); }
            }
        }
    }

    void MinimizeOnClicked(object sender, RoutedEventArgs args) => _overlapPresenter?.Minimize();

    void MaximizeOnClicked(object sender, RoutedEventArgs args) => _overlapPresenter?.Maximize();

    void CloseOnClicked(object sender, RoutedEventArgs args) => this.Close(); // -or- (Application.Current as App)?.Exit();

    #endregion

    #region [Helpers]
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
        };

        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(fe, spriteVisual);
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

            var ip = (IntPtr)AppWindow.Id.Value;
            if (ip != IntPtr.Zero)
            {
                InitializeWithWindow.Initialize(secondaryTile, (IntPtr)AppWindow.Id.Value);
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
}
