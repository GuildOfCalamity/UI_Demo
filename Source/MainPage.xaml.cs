using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;

using WinRT.Interop;

namespace UI_Demo;

/// <summary>
/// The main content, which is loaded into MainWindow on launch.
/// </summary>
public sealed partial class MainPage : Page, INotifyPropertyChanged
{
    #region [Props]
    bool _loaded = false;
    bool _useSpinner = true;
    bool _useSurfaceBrush = true;
    static Brush? _lvl1, _lvl2, _lvl3, _lvl4, _lvl5;
    DispatcherTimer? _flyoutTimer;
    CancellationTokenSource? _ctsTask;

    bool useSpringInsteadOfScalar = true;
    float _springMultiplier = 1.05f;
    SpringVector3NaturalMotionAnimation? _springVectorAnimation;
    SpringScalarNaturalMotionAnimation? _springScalarAnimation;
    
    PointLight? _pointLightBanner, _pointLightButton;

    readonly int _maxMessages = 50;
    readonly ObservableCollection<ApplicationMessage>? _coreMessages;
    readonly DispatcherQueue _localDispatcher;

    public Action? ProgressButtonClickEvent { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; NotifyPropertyChanged(nameof(IsBusy)); }
    }
    double _amount = 0;
    public double Amount
    {
        get => _amount;
        set { _amount = value; NotifyPropertyChanged(nameof(Amount)); }
    }
    double _size = 0;
    public double Size
    {
        get => _size;
        set { _size = value; NotifyPropertyChanged(nameof(Size)); }
    }
    string _status = string.Empty;
    public string Status
    {
        get => _status;
        set { _status = value; NotifyPropertyChanged(nameof(Status)); }
    }
    DelayTime _delay = DelayTime.Medium;
    public DelayTime Delay
    {
        get => _delay;
        set { _delay = value; NotifyPropertyChanged(nameof(Delay)); }
    }
    public ObservableCollection<string> LogMessages { get; private set; } = new();
    public ObservableCollection<string> Assets = new();
    public void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName)) { return; }
        // Confirm that we're on the UI thread in the event that DependencyProperty is changed under forked thread.
        DispatcherQueue.InvokeOnUI(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    public int ProcessorCount
    {
        get
        {
            try
            {
                return App.MachineEnvironment["NUMBER_OF_PROCESSORS"] switch
                {
                    "64" => 64,
                    "32" => 32,
                    "28" => 28,
                    "24" => 24,
                    "20" => 20,
                    "18" => 18,
                    "16" => 16,
                    "14" => 14,
                    "12" => 12,
                    "10" => 10,
                    "8" => 8,
                    "6" => 6,
                    "4" => 4,
                    "2" => 2,
                    "1" => 1,
                    _ => 0
                };
            }
            catch (KeyNotFoundException) { return 2; }
        }
    }
    #endregion

    public ICommand SwitchDelayCommand { get; private set; }
    public ICommand ProgressCommand { get; private set; }

    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += MainPageOnLoaded;
        this.Unloaded += MainPageOnUnloaded;
        MessageSplitView.PaneOpened += OnPaneOpenedOrClosed;
        MessageSplitView.PaneClosed += OnPaneOpenedOrClosed;
        App.WindowSizeChanged += SizeChangeEvent;
        PopulateAssets();

        #region [Action example for our ProgressButton control]
        ProgressButtonClickEvent += async () =>
        {
            tbMessages.DispatcherQueue.TryEnqueue(() => tbMessages.Text = "Running…");
            IsBusy = true;
            DispatcherQueue.TryEnqueue(() => { CustomTooltip.IsOpen = IsBusy; });
            Amount = 0;
            for (int i = 0; i < 100; i++)
            {
                Amount += 1;
                switch (Delay)
                {
                    case DelayTime.Short: await Task.Delay(6); break;
                    case DelayTime.Medium: await Task.Delay(18); break;
                    case DelayTime.Long: await Task.Delay(40); break;
                    default: await Task.Delay(10); break;
                }
            }
            tbMessages.DispatcherQueue.TryEnqueue(() => tbMessages.Text = "Finished");
            IsBusy = false;
            DispatcherQueue.TryEnqueue(() => { CustomTooltip.IsOpen = IsBusy; });
            //var dr = await DialogHelper.ShowAsync(new Dialogs.ResultsDialog("Action Result", "Progress button's action event was completed.", MessageLevel.Information), Content as FrameworkElement);
        };
        #endregion

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

        _localDispatcher = DispatcherQueue.GetForCurrentThread();
        _coreMessages = new();
        Binding binding = new Binding { Mode = BindingMode.OneWay, Source = _coreMessages };
        BindingOperations.SetBinding(lvChannelMessages, ListView.ItemsSourceProperty, binding);

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        _pointLightButton = _compositor?.CreatePointLight();
        _pointLightBanner = _compositor?.CreatePointLight();

        #region [Button Animation]
        // You could also use Behaviors for this, but I wanted to
        // keep this solution "pure" and not use any additional NuGets
        // apart from the core SDK that's required for a WinUI3 app.

        if (useSpringInsteadOfScalar)
        {   
            // SpringVector3NaturalMotionAnimation
            btnRun.PointerEntered += RunButtonOnPointerEntered;
            btnRun.PointerExited += RunButtonOnPointerExited;
        }
        else
        {   
            // SpringScalarNaturalMotionAnimation
            float btnOffsetX = 0; // Store the button's initial offset for later animations.
            btnRun.Loaded += (s, e) => 
            {   // It seems when the button's offset is modified from Grid/Stack centering,
                // we must force an animation to run to setup the initial starting conditions.
                // If you skip this step then you'll have to mouse-over the button twice to
                // see the intended animation (for first run only).
                btnOffsetX = btnRun.ActualOffset.X;
                AnimateButtonX(btnRun, btnOffsetX);
            };
            btnRun.PointerEntered += (s, e) => { AnimateButtonX(btnRun, btnOffsetX + 4f); };
            btnRun.PointerExited += (s, e) => { AnimateButtonX(btnRun, btnOffsetX); };
        }
        #endregion

        // Configure delay time relay command.
        SwitchDelayCommand = new AsyncRelayCommand(async (param) =>
        {
            if (param is string str)
            {
                if (Enum.IsDefined(typeof(DelayTime), str))
                {
                    // For testing IsEnabled="{Binding SwitchDelayCommand.CanExecute}"
                    await Task.Delay(250 * (App.IsCapsLockOn() ? 8 : 1));

                    Delay = (DelayTime)Enum.Parse(typeof(DelayTime), str);
                }
                else
                {
                    Debug.WriteLine($"[WARNING] Parameter is not of type '{nameof(DelayTime)}'.");
                }
            }
            else
            {
                Debug.WriteLine($"[WARNING] Parameter was not a string, nothing to do.");
            }
        });

        #region [ColorAnimationHelper test]
        BasicButton.PointerEntered += (s, e) =>
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            (s as Button)!.FontWeight = Microsoft.UI.Text.FontWeights.Bold;

            //var vis = ElementCompositionPreview.GetElementChildVisual(s as Button);
            //if (vis is not null)
            //    ElementCompositionPreview.SetElementChildVisual(s as Button, null);

            ColorAnimationHelper.CreateOrStartAnimation(s as Button, Colors.White, Colors.DodgerBlue, TimeSpan.FromSeconds(0.8));
            //(s as Button)!.Background = ColorAnimationHelper.CreateLinearGradientBrush(Colors.DodgerBlue, Colors.WhiteSmoke);
        };
        BasicButton.PointerExited += (s, e) =>
        {
            ProtectedCursor = null;
            (s as Button)!.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            ColorAnimationHelper.StopAnimation(s as Button);
            //(s as Button)!.Background = ColorAnimationHelper.CreateLinearGradientBrush(Colors.WhiteSmoke, Colors.DodgerBlue);
        };
        //BasicButton.Tapped += (s, e) =>
        //{
        //    ColorAnimationHelper.StopAnimation(s as Button);
        //    ColorAnimationHelper.CreateAndStartOneTimeAnimation(s as Button, Colors.White, Colors.Orchid, TimeSpan.FromSeconds(0.25));
        //};
        #endregion

        #region [Testing IAsyncOperations]
        ProgressCommand = new AsyncRelayCommand(async (obj) =>
        {
            try
            {
                IAsyncOperationWithProgress<ulong, ulong>? iaop = PerformDownloadAsync(_delay, _ctsTask == null ? new CancellationTokenSource().Token : _ctsTask.Token);
                iaop.Progress = (result, prog) =>
                {
                    if (iaop.Status != AsyncStatus.Completed)
                        DispatcherQueue.TryEnqueue(() => { tbVersion.Text = $"Progress: {prog}%"; });
                    else
                        DispatcherQueue.TryEnqueue(() => { tbVersion.Text = $"AsyncStatus: {iaop.Status}"; });
                };
                var result = await iaop;
                DispatcherQueue.TryEnqueue(() => { tbVersion.Text = $"AsyncStatus(final): {iaop.Status}"; });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() => tbVersion.Text = $"AsyncStatus(error): {ex.Message}");
            }
        });
        #endregion

        // WindowsXamlManager is part of the Windows App SDK XAML hosting API. This API enables non-WinAppSDK
        // desktop applications to host any control that derives from Microsoft.UI.Xaml.UIElement in a UI element
        // that is associated with a window handle (HWND). This API can be used by desktop applications built
        // using WPF, Windows Forms, and the Windows API (Win32). We're not using it in this application since
        // it is already a WinUI3 app, but as an example I am hooking the only event the manager exposes.
        // This event succeeds the AppWindow.Destroying() event.
        Microsoft.UI.Xaml.Hosting.WindowsXamlManager? wxm = Microsoft.UI.Xaml.Hosting.WindowsXamlManager.GetForCurrentThread();
        wxm.XamlShutdownCompletedOnThread += (s, e) => { Debug.WriteLine($"[INFO] XamlShutdownCompleted"); };
    }

    void ToggleFadeTooltip() => CustomTooltip.IsOpen = !CustomTooltip.IsOpen;

    #region [Events]
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

            // This only worked after adding <AllowUnsafeBlocks> to the csproj.
            //Flipper.ItemsSource = Assets;

            if (Assets.Count > 0)
            {
                // We can also create a binding in code-behind.
                Binding binding = new Binding { Mode = BindingMode.OneWay, Source = Assets };
                BindingOperations.SetBinding(Flipper, FlipView.ItemsSourceProperty, binding);
            }
            else
                UpdateInfoBar($"No assets for binding to FlipView", MessageLevel.Warning);

            // Start channel test. (pub sub is the better option now)
            //_ = Task.Run(async () => await StartListeningToGenericMessageServiceAsync(App.CoreChannelToken.Token));

            // Subscribe to app-wide messages.
            PubSubEnhanced<ApplicationMessage>.Instance.Subscribe(OnPubSubReceived);

            try
            {
                var tag1 = App.MachineEnvironment["PROCESSOR_IDENTIFIER"];
                var tag2 = App.MachineEnvironment["NUMBER_OF_PROCESSORS"];
                //Debug.WriteLine($"[INFO] User profile is located here: '{App.MachineEnvironment["USERPROFILE"]}'");
                UpdateInfoBar($"Using {tag1} with {tag2} processors", MessageLevel.Information);
            }
            catch (KeyNotFoundException) { }

            #region [PointLight animations]
            if (_pointLightButton != null)
            {
                ColorKeyFrameAnimation? ckfa1 = _compositor?.CreateColorKeyFrameAnimation();
                ckfa1.InsertKeyFrame(0.0f, Colors.LightGray);
                ckfa1.InsertKeyFrame(0.33f, Colors.DodgerBlue);
                ckfa1.InsertKeyFrame(0.66f, Colors.LightBlue);
                ckfa1.InsertKeyFrame(1.0f, Colors.White);
                ckfa1.Duration = TimeSpan.FromSeconds(3);
                ckfa1.IterationBehavior = AnimationIterationBehavior.Forever;
                ckfa1.DelayTime = TimeSpan.FromSeconds(0);
                ckfa1.Direction = Microsoft.UI.Composition.AnimationDirection.Alternate;
                _pointLightButton.Offset = new Vector3((float)tbMessagePane.ActualWidth / 2, (float)tbMessagePane.ActualHeight / 2, 100f);
                _pointLightButton.CoordinateSpace = ElementCompositionPreview.GetElementVisual(tbMessagePane);
                _pointLightButton.Targets.Add(ElementCompositionPreview.GetElementVisual(tbMessagePane));
                _pointLightButton?.StartAnimation("Color", ckfa1);

                ColorKeyFrameAnimation? ckfa2 = _compositor?.CreateColorKeyFrameAnimation();
                ckfa2.InsertKeyFrame(0.0f, Colors.White);
                ckfa2.InsertKeyFrame(0.3f, Colors.SkyBlue);
                ckfa2.InsertKeyFrame(0.6f, Colors.DodgerBlue);
                ckfa2.InsertKeyFrame(1.0f, Colors.White);
                ckfa2.Duration = TimeSpan.FromSeconds(1.9);
                ckfa2.IterationBehavior = AnimationIterationBehavior.Forever;
                ckfa2.DelayTime = TimeSpan.FromSeconds(0);
                ckfa2.Direction = Microsoft.UI.Composition.AnimationDirection.Alternate;
                _pointLightBanner.Offset = new Vector3((float)tbMessages.ActualWidth / 2, (float)tbMessages.ActualHeight / 2, 100f);
                _pointLightBanner.CoordinateSpace = ElementCompositionPreview.GetElementVisual(tbMessages);
                _pointLightBanner.Targets.Add(ElementCompositionPreview.GetElementVisual(tbMessages));
                _pointLightBanner?.StartAnimation("Color", ckfa2);
            }
            #endregion

            #region [Fetch previous messages]
            try
            {
                int count = 0;
                var prevMsgs = App.MessageLog?.GetData();
                if (prevMsgs is not null)
                {
                    foreach (var msg in prevMsgs)
                    {
                        // Load messages as long as we aren't exceeding the limit and they're fresh.
                        if (++count < _maxMessages && !msg.MessageTime.IsOlderThanDays(4d))
                            _coreMessages.Add(msg);
                    }
                    Debug.WriteLine($"[INFO] {count} previous messages loaded");
                }
            }
            catch (Exception) { Debug.WriteLine($"[WARNING] Failed to load previous message list."); }
            #endregion

            _ = Task.Run(async () => 
            { 
                while (!App.IsClosing)
                {
                    await Task.Delay(500);
                    Size = Math.Round((DateTime.Now.Second / 60.0) * 100, 0, MidpointRounding.ToEven);
                }
            });
        }
        _loaded = true;
    }

    void MainPageOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_pointLightButton != null)
        {
            _pointLightButton?.StopAnimation("Color");
            if (_pointLightButton.Targets.Count > 0)
                _pointLightButton.Targets.RemoveAll();

            _pointLightBanner?.StopAnimation("Color");
            if (_pointLightBanner.Targets.Count > 0)
                _pointLightBanner.Targets.RemoveAll();
        }

        PubSubEnhanced<ApplicationMessage>.Instance.Unsubscribe(OnPubSubReceived);

        if (_coreMessages.Count > 1)
            App.MessageLog?.SaveData(_coreMessages?.ToList());
    }

    /// <summary>
    /// <see cref="SpringVector3NaturalMotionAnimation"/>
    /// </summary>
    void RunButtonOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var btn = sender as Button;
        CreateOrUpdateSpringVectorAnimation(_springMultiplier);
        var uie = sender as UIElement;
        if (uie != null)
        {   // We'll set the CenterPoint so the SpringAnimation does not start from offset 0,0.
            uie.CenterPoint = new System.Numerics.Vector3((float)(btn.ActualWidth / 2.0), (float)(btn.ActualHeight / 2.0), 1f);
            uie.StartAnimation(_springVectorAnimation);
        }
    }

    /// <summary>
    /// <see cref="SpringVector3NaturalMotionAnimation"/>
    /// </summary>
    void RunButtonOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var btn = sender as Button;
        CreateOrUpdateSpringVectorAnimation(1.0f);
        var uie = sender as UIElement;
        if (uie != null)
        {   // We'll set the CenterPoint so the SpringAnimation does not start from offset 0,0.
            uie.CenterPoint = new System.Numerics.Vector3((float)(btn.ActualWidth / 2.0), (float)(btn.ActualHeight / 2.0), 1f);
            uie.StartAnimation(_springVectorAnimation);
        }
    }

    void SizeChangeEvent(Windows.Graphics.SizeInt32 newSize)
    {
        if (this.Content != null)
        {
            tsBlur.DispatcherQueue.TryEnqueue(() =>
            {
                if (tsBlur.IsOn && (bool)cbTest.IsChecked)
                {
                    // Force the SpriteVisual to be recreated.
                    if (_blurVisual != null)
                    {
                        tsBlur.IsOn = false;
                        _blurVisual.Dispose();
                        _blurVisual = null;
                    }
                }
            });
        }
        else
        {
            Debug.WriteLine($"[WARNING] Page content is not ready yet.");
        }
    }

    void ButtonOnClick(object sender, RoutedEventArgs e)
    {
        bool useTechnique1 = false;
        
        _ctsTask = new CancellationTokenSource();

        var delay = Delay;
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
                switch (delay)
                {
                    case DelayTime.Short: await Task.Delay(1500); break;
                    case DelayTime.Medium: await Task.Delay(3000); break;
                    case DelayTime.Long: await Task.Delay(5000); break;
                    default: await Task.Delay(500); break;
                }
            }, _ctsTask.Token);

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
                    tbMessages.Text = "Process complete!";
                    _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog("Process complete", "Button's click event was completed.", MessageLevel.Information), Content as FrameworkElement);
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

            ToggleAnimation(true);

            var dummyTask = SampleAsyncMethod(_ctsTask.Token);

            #region [ContinueWith]
            /** Happens when successful **/
            dummyTask.ContinueWith(task =>
            {
                var list = task.Result; // Never access Task.Result unless the Task was successful.
                foreach (var thing in list) { LogMessages.Add(thing); }
                UpdateInfoBar("Technique #2 was completed.", MessageLevel.Information);
                _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog("Task Result", "Technique #2 ran to completion.", MessageLevel.Information), Content as FrameworkElement);

            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());

            /** Happens when faulted **/
            dummyTask.ContinueWith(task =>
            {
                foreach (var ex in task.Exception!.Flatten().InnerExceptions) { tbMessages.Text = ex.Message; }
                UpdateInfoBar("Technique #2 was completed.", MessageLevel.Error);
                _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog("Task Result", "Technique #2 has faulted.", MessageLevel.Error), Content as FrameworkElement);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());

            /** Happens when canceled **/
            dummyTask.ContinueWith(task =>
            {
                tbMessages.Text = "Dummy Process Canceled!";
                UpdateInfoBar("Technique #2 was completed.", MessageLevel.Warning);
                _ = DialogHelper.ShowAsTask(new Dialogs.ResultsDialog("Task Result", "Technique #2 was canceled.", MessageLevel.Warning), Content as FrameworkElement);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());

            /** Always happens **/
            dummyTask.ContinueWith(task =>
            {
                IsBusy = false;
                ToggleAnimation(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            #endregion

            // Just a place-holder for this demo.
            async Task<List<string>> SampleAsyncMethod(CancellationToken cancelToken)
            {
                var list = new List<string>();
                for (int i = 1; i < 101; i++)
                {
                    list.Add($"Item {i}");
                  
                    switch (delay)
                    {
                        case DelayTime.Short: await Task.Delay(20); break;
                        case DelayTime.Medium: await Task.Delay(50); break;
                        case DelayTime.Long: await Task.Delay(100); break;
                        default: await Task.Delay(10); break;
                    }

                    int diceRoll = Random.Shared.Next(200);
                    
                    if (diceRoll == 199)
                        _ctsTask.Cancel();
                    else if (diceRoll == 198)
                        throw new Exception("This is a fake exception");

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
            if ((bool)tb.IsChecked && !MessageSplitView.IsPaneOpen)
                MessageSplitView.IsPaneOpen = true;
            else
                MessageSplitView.IsPaneOpen = false;
        }
    }

    /// <summary>
    /// The <see cref="SplitView"/> can be dismissed when the user clicks anywhere else on the 
    /// page, so we'll make sure the <see cref="ToggleButton"/> reflects that current state.
    /// </summary>
    void OnPaneOpenedOrClosed(SplitView sender, object args)
    {
        Debug.WriteLine($"[INFO] {sender.Name} was {((MessageSplitView.IsPaneOpen) ? "opened" : "closed")}");
        if (!MessageSplitView.IsPaneOpen && (bool)tbMessagePane.IsChecked)
        {
            tbMessagePane.IsChecked = false;
        }
    }

    async void OnSwitchToggled(object sender, RoutedEventArgs e)
    {
        bool useImageControl = false;

        var ts = sender as ToggleSwitch;
        if (this.Content != null && ts != null) // use as "is loaded" check for the MainWindow
        {
            var ctrlPosition = ts.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point(0d, 0d));
            Debug.WriteLine($"[INFO] Relative control position: {ctrlPosition.X},{ctrlPosition.Y}");
            if (ts.IsOn)
            {
                // Capture an image of the current page.
                var bmp = await AppCapture.GetScreenshot(root);

                if (useImageControl)
                {
                    blurTest.Width = root.ActualWidth;
                    blurTest.Height = root.ActualHeight;
                    blurTest.Source = await BlurHelper.ApplyBlurAsync(bmp);
                    blurTest.Visibility = Visibility.Visible;
                }
                else
                {
                    if (bmp != null && _useSurfaceBrush)
                    {
                        Debug.WriteLine($"[INFO] We have a software bitmap with dimensions {bmp.PixelWidth},{bmp.PixelHeight}");

                        // We'll need to force a new CompositionSurfaceBrush since we'll be replacing it with our new blurred screenshot.
                        if (_blurVisual != null)
                        {
                            _blurVisual.Dispose();
                            _blurVisual = null;
                        }
#if IS_UNPACKAGED
                        string assetPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "BlurTest.png");
#else
                        string assetPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "BlurTest.png");
#endif
                        // Create a new blur image for the CompositionSurfaceBrush.
                        var saved = await BlurHelper.ApplyBlurAndSaveAsync(bmp, assetPath, 5);

                        // This works properly, but only because we use an Image control as an intermediate.
                        // https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-22621#examples
                        //await AppCapture.SaveImageSourceToFileAsync(root, blurTest, blurred, assetPath, App.m_width, App.m_height);

                        AddBlurCompositionElement(root, new Windows.UI.Color() { A = 255, R = 20, G = 20, B = 32 }, useSurfaceBrush: saved, useImageForShadowMask: false);
                    }
                    else
                    {
                        // Just use the "fog" effect if image routines are not needed - this effect is not as fancy, but it's much faster.
                        AddBlurCompositionElement(root, new Windows.UI.Color() { A = 255, R = 20, G = 20, B = 32 }, useSurfaceBrush: false, useImageForShadowMask: false);
                    }
                }
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
            if (App.Profile != null)
                App.Profile.LastCount = (int)e.NewValue;

            if (((int)e.NewValue == sld.Minimum || (int)e.NewValue == sld.Maximum) && _loaded)
            {
                //ContentDialogResult result = await DialogHelper.ShowAsync(new Dialogs.RecreationDialog(), this.Content as FrameworkElement);
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

    /// <summary>
    /// <see cref="PubSubEnhanced{T}"> testing.
    /// </summary>
    void OnPubSubReceived(ApplicationMessage msg)
    {
        if (_loaded && msg != null)
        {
            _localDispatcher.TryEnqueue(() =>
            {
                if (_coreMessages.Count > _maxMessages)
                    _coreMessages.RemoveAt(_maxMessages);
            
                _coreMessages?.Insert(0, msg);
            });
        }
    }

    /// <summary>
    /// Test for pinning app via <see cref="SecondaryTile"/>.
    /// Maybe this only works with packaged apps?
    /// </summary>
    async void ToolbarButtonOnClick(object sender, RoutedEventArgs e)
    {
        bool isPinnedSuccessfully = false;
        try
        {
            flyoutText.ShowAt(sender as Button);

            string tag = App.GetCurrentNamespace() ?? "WinUI Demo";

            Windows.UI.StartScreen.SecondaryTile secondaryTile = new Windows.UI.StartScreen.SecondaryTile($"WinUI_{tag}");
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

    void flyoutTextOnOpened(object sender, object e)
    {
        if (flyoutText.IsOpen)
        {
            if (_flyoutTimer != null) { _flyoutTimer.Stop(); }
            // If we had an existing timer just re-create it.
            _flyoutTimer = new DispatcherTimer();
            _flyoutTimer.Interval = TimeSpan.FromSeconds(4);
            _flyoutTimer.Tick += (_, _) =>
            {
                flyoutText.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    if (flyoutText.IsOpen)
                    {
                        flyoutText.Hide();
                        ToastHelper.ShowStandardToast(App.GetCurrentAssemblyName() ?? "WinUI3", "Flyout was closed via timer event.");
                    }
                    else
                    {
                        PubSubEnhanced<ApplicationMessage>.Instance.SendMessage(new ApplicationMessage
                        {
                            Module = ModuleId.MainPage,
                            MessageText = $"🔔 Notification toast was skipped",
                            MessageType = typeof(string),
                        });
                    }
                });
                if (_flyoutTimer != null) { _flyoutTimer.Stop(); }
            };
            _flyoutTimer.Start();
        }
    }

    void TextBoxOnKeyUp(object sender, KeyRoutedEventArgs e) => UpdateInfoBar($"Key press: '{e.Key}'", MessageLevel.Information);

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
                // Reset first run flag.
                App.Profile!.FirstRun = true;
            }
            else if (!string.IsNullOrEmpty(tag) && tag.Equals("ActionToken", StringComparison.OrdinalIgnoreCase))
            {
                if (App.CoreChannelToken is not null)
                    App.CoreChannelToken.Cancel(); // Signal the channel cancellation token.

                if (_ctsTask is not null)
                    _ctsTask.Cancel(); // Signal the task cancellation token.
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

    void ButtonCompositePointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // [Immediate using no animation]
        //var transform = ((Button)sender).RenderTransform as CompositeTransform;
        //if (transform != null)
        //{
        //    transform.ScaleX = 1.111;
        //    transform.ScaleY = 1.111;
        //    transform.Rotation = 90;
        //}
        StartComboAnimation((Button)sender, 1.1, 200);
        ColorAnimationHelper.CreateOrStartAnimation(sender as Button, Colors.White, Colors.DodgerBlue, TimeSpan.FromSeconds(0.8));
    }

    void ButtonCompositePointerExited(object sender, PointerRoutedEventArgs e)
    {
        // [Immediate using no animation]
        //var transform = ((Button)sender).RenderTransform as CompositeTransform;
        //if (transform != null)
        //{
        //    transform.ScaleX = 1.0;
        //    transform.ScaleY = 1.0;
        //    transform.Rotation = 0;
        //}
        StartComboAnimation((Button)sender, 1.0, 200);
        ColorAnimationHelper.StopAnimation(sender as Button);
    }

    void ButtonCompositeTapped(object sender, TappedRoutedEventArgs e)
    {
        // [Immediate using no animation]
        //var transform = ((Button)sender).RenderTransform as CompositeTransform;
        //if (transform != null)
        //{
        //    transform.ScaleX = 0.9;
        //    transform.ScaleY = 0.9;
        //    transform.Rotation = 160;
        //}
        StartComboAnimation((Button)sender, 1.11, 100);
    }

    /// <summary>
    /// Uses the given scale to determine the angle of rotation.
    /// </summary>
    void StartComboAnimation(Button button, double scale, double ms)
    {
        CompositeTransform transform = button.RenderTransform as CompositeTransform;

        if (transform is null)
            return;

        // This could be reused by making a static global.
        Storyboard storyboard = new Storyboard();

        // X scale animation
        DoubleAnimation scaleXAnimation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(ms),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true,
            AutoReverse = false
        };

        // Y scale animation.
        DoubleAnimation scaleYAnimation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(ms),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true,
            AutoReverse = false
        };

        // Angle animation
        DoubleAnimation rotateAnimation = new DoubleAnimation
        {
            To = scale switch { 1.0 => 0, > 1.1 => 160, > 1.0 => 90, _ => 0 },
            Duration = TimeSpan.FromMilliseconds(ms),
            AutoReverse = scale switch { 1.0 => false, > 1.1 => true, > 1.0 => false, _ => false },
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };

        // NOTE: Use "ColorAnimationHelper.cs" for color animations.
        //ColorAnimation colorAnimation = new ColorAnimation
        //{
        //    To = scale switch { 1.0 => ((SolidColorBrush)button.Background).Color, > 1.1 => ((SolidColorBrush)button.Background).Color.LighterBy(0.25f), > 1.0 => ((SolidColorBrush)button.Background).Color.LighterBy(0.75f), _ => ((SolidColorBrush)button.Background).Color },
        //    Duration = TimeSpan.FromMilliseconds(ms),
        //    EnableDependentAnimation = true
        //};
        //Storyboard.SetTarget(colorAnimation, button);
        //Storyboard.SetTargetProperty(colorAnimation, "(Control.Background).(SolidColorBrush.Color)");

        /* Examples of some of commonly-used properties that use a Brush value include:
         *   (Control.Background).(SolidColorBrush.Color)
         *   (Control.Foreground).(SolidColorBrush.Color)
         *   (Shape.Fill).(SolidColorBrush.Color)
         *   (Control.BorderBrush).(SolidColorBrush.Color)
         *   (Panel.Background).(SolidColorBrush.Color)
         *   (TextBlock.Foreground).(SolidColorBrush.Color)
         */

        Storyboard.SetTarget(scaleXAnimation, transform);
        Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");
        Storyboard.SetTarget(scaleYAnimation, transform);
        Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
        Storyboard.SetTarget(rotateAnimation, transform);
        Storyboard.SetTargetProperty(rotateAnimation, "Rotation");

        //storyboard.Children.Add(colorAnimation);
        storyboard.Children.Add(scaleXAnimation);
        storyboard.Children.Add(scaleYAnimation);
        storyboard.Children.Add(rotateAnimation);
        storyboard.Begin();
    }
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

    void PopulateAssets()
    {
#if IS_UNPACKAGED
        string assetPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets");
#else
        string assetPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets");
#endif
        Assets.Clear();
        foreach (var f in Directory.GetFiles(assetPath, "*.png", SearchOption.TopDirectoryOnly))
        {
            Assets.Add(f);
        }
    }
    #endregion

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

        bool loadFromStream = false;

        CompositionSurfaceBrush? surfaceBrush = null;

        // Get the current compositor.
        if (_compositor == null)
            _compositor = ElementCompositionPreview.GetElementVisual(fe).Compositor;

        // If we're in the middle of an animation, cancel it now.
        if (_scopedBatch != null)
            CleanupScopeBatch();

        // If we've already created the sprite, just make it visible to save on memory usage.
        if (_blurVisual != null)
        {
            _blurVisual.IsVisible = true;
            BlurInVisualAnimation();
            return;
        }

        if (useSurfaceBrush)
        {   
            // Create surface brush and load image.
            surfaceBrush = _compositor.CreateSurfaceBrush();

            if (loadFromStream)
            {
                string assetPath = string.Empty;
                if (App.IsPackaged)
                    assetPath = System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "BlurTest.png");
                else
                    assetPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "BlurTest.png");
                if (File.Exists(assetPath))
                {
                    StorageFile? file = Windows.Storage.StorageFile.GetFileFromPathAsync(assetPath).GetAwaiter().GetResult();
                    using (var inStream = file.OpenReadAsync().GetAwaiter().GetResult())
                    {
                        var decoder = BitmapDecoder.CreateAsync(inStream).GetAwaiter().GetResult();
                        // Use an image as the shadow.
                        surfaceBrush.Surface = LoadedImageSurface.StartLoadFromStream(inStream);
                    }
                }
                else
                {
                    UpdateInfoBar($"'{assetPath}' was not found", MessageLevel.Warning);
                }
            }
            else
            {
                // Use an image as the shadow.
                surfaceBrush.Surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/BlurTest.png"));
            }

            //surfaceBrush.Scale = new Vector2 { X = 1.5f, Y = 1.5f };
            surfaceBrush.Stretch = CompositionStretch.UniformToFill;
            surfaceBrush.BitmapInterpolationMode = CompositionBitmapInterpolationMode.Linear;
        }

        // Create the destination sprite, sized to cover the entire page.
        _blurVisual = _compositor.CreateSpriteVisual();
        if (fe.ActualSize != Vector2.Zero)
            _blurVisual.Size = new Vector2((float)fe.ActualWidth+10, (float)fe.ActualHeight+14);
        else
            _blurVisual.Size = new Vector2((float)App.m_width, (float)App.m_height);

        if (surfaceBrush != null)
            _blurVisual.Brush = surfaceBrush;

        // Create drop shadow (this is noticeable on a CompositionSurfaceBrush, but not on a CompositionColorBrush).
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
        try
        {
            if (_scopedBatch != null)
            {
                _scopedBatch.Completed -= ScopeBatchCompleted;
                _scopedBatch.Dispose();
                _scopedBatch = null;
            }
        }
        catch (InvalidOperationException) { }
    }
    #endregion

    #region [Channel Methods]
    /// <summary>
    /// Reads messages from the core app channel and updates the UI.
    /// </summary>
    async Task ConsumerWaitToReadAsync(CancellationToken token)
    {
        if (App.CoreMessageChannel is null)
            return;

        int count = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Blocks until until data is available in the channel.
                if (await App.CoreMessageChannel.Reader.WaitToReadAsync(token))
                {
                    //var message = await _messageChannel.Reader.ReadAsync(token);
                    while (App.CoreMessageChannel.Reader.TryRead(out var message))
                    {
                        string formatted = $"📢 {DateTime.Now:T} – {message} #{++count:D3}";
                        _localDispatcher.TryEnqueue(() =>
                        {
                            if (_coreMessages.Count > _maxMessages)
                                _coreMessages.RemoveAt(_maxMessages);

                            _coreMessages?.Insert(0, new ApplicationMessage { MessageText = formatted });
                        });
                    }
                }

                // Wait for channel completion before notifying UI
                //await _messageChannel.Reader.Completion;
                //_localDispatcher.TryEnqueue(() => { _messages.Insert(0, "Channel completed, no more messages."); });
            }
            catch (OperationCanceledException)
            {
                if (!App.IsClosing)
                    UpdateInfoBar("Channel consumer was canceled!", MessageLevel.Warning);
                else
                    Debug.WriteLine("[WARNING] Channel consumer was canceled!");
            }
        }
    }

    /// <summary>
    /// Reads messages from the core app channel and updates the UI.
    /// </summary>
    async Task ConsumerReadAllAsync(CancellationToken token)
    {
        if (App.CoreMessageChannel is null)
            return;

        int count = 0;
        try
        {
            // Blocks until the channel receives a message or it canceled.
            await foreach (var message in App.CoreMessageChannel.Reader.ReadAllAsync(token))
            {
                string formatted = $"📢 {DateTime.Now:T} – {message} #{++count:D3}";
                _localDispatcher.TryEnqueue(() => _coreMessages?.Insert(0, new ApplicationMessage { MessageText = formatted }));
            }

            // Wait for channel completion before notifying UI
            //await _messageChannel.Reader.Completion;
            //_localDispatcher.TryEnqueue(() => { _messages.Insert(0, "Channel completed, no more messages."); });
        }
        catch (OperationCanceledException)
        {
            if (!App.IsClosing)
                UpdateInfoBar("Channel consumer was canceled!", MessageLevel.Warning);
            else
                Debug.WriteLine("[WARNING] Channel consumer was canceled!");
        }
    }
    /// <summary>
    /// Test channel listeners.
    /// </summary>
    void SendMessageToStringService(CancellationToken token = default)
    {   // Send a test message to non-generic consumer
        _ = MessageService.Instance.SendMessageAsync("🚀 WinUI 3 Channel Test Started!", token);
    }
    async Task StartListeningToMessageServiceAsync(CancellationToken token = default)
    {
        var reader = MessageService.Instance.GetMessageReader();
        try
        {
            await foreach (var message in reader.ReadAllAsync(token))
            {
                if (_coreMessages is not null)
                {
                    _localDispatcher.TryEnqueue(() =>
                    {
                        if (_coreMessages.Count > _maxMessages)
                            _coreMessages.RemoveAt(_maxMessages);

                        _coreMessages?.Insert(0, new ApplicationMessage { MessageText = message });
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            _localDispatcher.TryEnqueue(() => _coreMessages?.Insert(0, new ApplicationMessage { MessageText = "⚠️ UI Consumer Canceled." }));
        }
    }

    /// <summary>
    /// Test channel listeners. (generic version)
    /// </summary>
    async Task SendNumberToGenericMessageService(CancellationToken token = default)
    {   // Send a numeric code to generic consumer
        await MessageService<ChannelMessageType>.Instance.SendMessageAsync(Extensions.GetRandomEnum<ChannelMessageType>(), token);
    }
    async Task StartListeningToGenericMessageServiceAsync(CancellationToken token = default)
    {
        int count = 0;
        var reader = MessageService<ChannelMessageType>.Instance.GetMessageReader();
        //_maxMessages = MessageService<ChannelMessageType>.Instance.GetMaxmimumLimit();
        try
        {
            await foreach (var message in reader.ReadAllAsync(token))
            {
                if (_coreMessages is not null)
                {
                    _localDispatcher.TryEnqueue(() =>
                    {
                        if (_coreMessages.Count > _maxMessages)
                            _coreMessages.RemoveAt(_maxMessages);

                        string formatted = $"📢 {DateTime.Now:T} – {message} #{++count:D3}";
                        _coreMessages?.Insert(0, new ApplicationMessage { MessageText = $"{formatted}" });
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (!App.IsClosing)
                UpdateInfoBar("Generic channel consumer was canceled!", MessageLevel.Warning);
            else
                Debug.WriteLine("[WARNING] Generic channel consumer was canceled!");
        }
    }
    #endregion

    #region [SpringScalar and SpringVector Animations]
    void CreateOrUpdateSpringVectorAnimation(float finalValue)
    {
        if (_springVectorAnimation == null && _compositor != null)
        {
            // When updating targets such as "Position" use a Vector3KeyFrameAnimation.
            //var positionAnim = _compositor.CreateVector3KeyFrameAnimation();
            // When updating targets such as "Opacity" use a ScalarKeyFrameAnimation.
            //var sizeAnim = _compositor.CreateScalarKeyFrameAnimation();

            _springVectorAnimation = _compositor.CreateSpringVector3Animation();
            _springVectorAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            _springVectorAnimation.Target = "Scale";
            _springVectorAnimation.InitialVelocity = new System.Numerics.Vector3(_springMultiplier);
            _springVectorAnimation.DampingRatio = 0.3f;  // Lower values are more "springy"
            _springVectorAnimation.Period = TimeSpan.FromMilliseconds(50);
        }

        if (_springVectorAnimation != null)
            _springVectorAnimation.FinalValue = new System.Numerics.Vector3(finalValue);
    }

    /// <summary>
    /// Ensures the button starts with Offset.Y = 0
    /// </summary>
    public void InitializeButtonOffsetY(Button button)
    {
        if (button == null) { return; }
        Visual buttonVisual = ElementCompositionPreview.GetElementVisual(button);
        buttonVisual.Offset = new Vector3(buttonVisual.Offset.X, 0, buttonVisual.Offset.Z);
    }
    public void AnimateButtonY(Button button, float offset)
    {
        if (button == null)
            return;

        if (_compositor == null)
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        // Create a Spring Animation for Y offset
        SpringScalarNaturalMotionAnimation _springAnimation = _compositor.CreateSpringScalarAnimation();
        _springAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        _springAnimation.Target = "Offset.Y";   // Move vertically
        _springAnimation.InitialVelocity = 50f; // Adjust movement speed
        _springAnimation.FinalValue = offset;   // Set the final target Y position
        _springAnimation.DampingRatio = 0.3f;   // Lower values are more "springy"
        _springAnimation.Period = TimeSpan.FromMilliseconds(50);

        // Get the button's visual and apply the animation
        Visual buttonVisual = ElementCompositionPreview.GetElementVisual(button);
        buttonVisual.StartAnimation("Offset.Y", _springAnimation);
    }

    /// <summary>
    /// Ensures the button starts with Offset.X = 0
    /// </summary>
    public void InitializeButtonOffsetX(Button button)
    {
        if (button == null) { return; }
        Visual buttonVisual = ElementCompositionPreview.GetElementVisual(button);
        buttonVisual.Offset = new Vector3(0, buttonVisual.Offset.Y, buttonVisual.Offset.Z);
    }
    public void AnimateButtonX(Button button, float offset)
    {
        if (button == null)
            return;

        if (_compositor == null)
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        // Create a Spring Animation for X offset
        SpringScalarNaturalMotionAnimation _springAnimation = _compositor.CreateSpringScalarAnimation();
        _springAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        _springAnimation.Target = "Offset.X";   // Move vertically
        _springAnimation.InitialVelocity = 50f; // Adjust movement speed
        _springAnimation.FinalValue = offset;   // Set the final target X position
        _springAnimation.DampingRatio = 0.3f;   // Lower values are more "springy"
        _springAnimation.Period = TimeSpan.FromMilliseconds(50);

        // Get the button's visual and apply the animation
        Visual buttonVisual = ElementCompositionPreview.GetElementVisual(button);
        buttonVisual.StartAnimation("Offset.X", _springAnimation);
    }

    /// <summary>
    /// Ensures the grid starts with Offset.X = 0
    /// </summary>
    public void InitializeGridOffsetX(Grid grid)
    {
        if (grid == null) { return; }
        Visual gridVisual = ElementCompositionPreview.GetElementVisual(grid);
        gridVisual.Offset = new System.Numerics.Vector3(0, gridVisual.Offset.Y, gridVisual.Offset.Z);
    }
    public void AnimateGridX(Grid grid, float offset)
    {
        if (grid == null)
            return;

        if (_compositor == null)
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        // Create a Spring Animation for X offset
        SpringScalarNaturalMotionAnimation _springAnimation = _compositor.CreateSpringScalarAnimation();
        _springAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        _springAnimation.Target = "Offset.X";   // Move vertically
        _springAnimation.InitialVelocity = 50f; // Adjust movement speed
        _springAnimation.FinalValue = offset;   // Set the final target X position
        _springAnimation.DampingRatio = 0.3f;   // Lower values are more "springy"
        _springAnimation.Period = TimeSpan.FromMilliseconds(50);

        // Get the button's visual and apply the animation
        Visual gridVisual = ElementCompositionPreview.GetElementVisual(grid);
        gridVisual.StartAnimation("Offset.X", _springAnimation);
    }
    #endregion

    #region [IAsyncOperation]
    static Windows.Storage.FileProperties.BasicProperties lastProps;
    public IAsyncOperation<Windows.Storage.FileProperties.BasicProperties> GetBasicFilePropertiesAsync(string name, StreamedFileDataRequestedHandler handler)
    {
        return AsyncInfo.Run(async (token) =>
        {
            async Task<Windows.Storage.FileProperties.BasicProperties> GetFakeBasicProperties()
            {
                var streamedFile = await StorageFile.GetFileFromPathAsync(name);
                return await streamedFile.GetBasicPropertiesAsync();
            }
            return lastProps ?? (lastProps = await GetFakeBasicProperties());
        });
    }

    public async Task<FileSystemItemType> GetTypeFromPath(string path)
    {
        IStorageItem item = await StorageFile.GetFileFromPathAsync(path);
        return item is null ? FileSystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FileSystemItemType.Directory : FileSystemItemType.File);
    }

    public async Task<long> GetFileSize(IStorageFile file)
    {
        Windows.Storage.FileProperties.BasicProperties properties = await file.GetBasicPropertiesAsync();
        return (long)properties.Size;
    }

    public async Task TestAndThenCopy(string origFilePath, string copyFileName)
    {
        await StorageFile.GetFileFromPathAsync(origFilePath)
            .AsTask()
            .AndThen(c => 
            c.CopyAsync(ApplicationData.Current.TemporaryFolder, copyFileName, NameCollisionOption.ReplaceExisting)
            .AsTask());
    }

    /// <summary>
    /// Download simulation using <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>.
    /// </summary>
    public IAsyncOperationWithProgress<ulong, ulong> PerformDownloadAsync(DelayTime delay, CancellationToken token = default)
    {
        return AsyncInfo.Run<ulong, ulong>((token, progress) =>
        {
            return Task<ulong>.Run(async () =>
            {
                ulong length = 0;
                for (int i = 0; i < 100; i++)
                {
                    if (!token.IsCancellationRequested)
                    {
                        if (length < 100)
                            length++;
                        else
                            length = 100;

                        progress.Report(length);

                        // fake delay to simulate download process
                        switch (delay)
                        {
                            case DelayTime.Short: await Task.Delay(10); break;
                            case DelayTime.Medium: await Task.Delay(30); break;
                            case DelayTime.Long: await Task.Delay(80); break;
                            default: await Task.Delay(10); break;
                        }
                    }
                    else
                        break;
                }
                return length;
            });
        });
    }
    #endregion
}
