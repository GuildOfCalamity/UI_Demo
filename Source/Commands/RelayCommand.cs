﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

namespace UI_Demo;

#region [Basic]
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;
    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
    public void Execute(object parameter) => _execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object, Task> _executeAsync;
    private readonly Func<object, bool> _canExecute;
    private bool _isExecuting;
    public event EventHandler? CanExecuteChanged;

    public AsyncRelayCommand(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }
    public bool CanExecute(object parameter) => !_isExecuting && (_canExecute == null || _canExecute(parameter));
    public async void Execute(object parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _executeAsync(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
#endregion


#region [Advanced]
internal interface IAsyncCommandDelegate<TParameter, TResult>
{
    Task<TResult> Execute(TParameter? parameter);
    bool CanExecute(TParameter? parameter);
}

/// <summary>
///   ICommand implementation to use Task async callbacks
/// </summary>
/// <typeparam name="TParameter">Command parameter type</typeparam>
/// <typeparam name="TResult">Result parameter type</typeparam>
internal partial class AsyncCommand<TParameter, TResult> : ModelBase, ICommand
{
    private Task? _executeTask;
    private TResult? _result;
    private Exception? _error;
    private bool _canExecute;

    protected IAsyncCommandDelegate<TParameter, TResult> AsyncCommandDelegate { get; }

    internal AsyncCommand(IAsyncCommandDelegate<TParameter, TResult> asyncCommandDelegate)
    {
        AsyncCommandDelegate = asyncCommandDelegate;
    }

    internal AsyncCommand(
        Func<TParameter?, Task<TResult>> executeCallback,
        Func<TParameter?, bool> canExecuteCallback)
        : this(new DefaultAsyncCommandDelegate<TParameter, TResult>(executeCallback, canExecuteCallback))
    {
    }

    internal AsyncCommand(
        Func<TParameter?, TResult> executeCallback,
        Func<TParameter?, bool> canExecuteCallback)
        : this(ToCallbackAsync(executeCallback), canExecuteCallback)
    {
    }

    public event EventHandler? CanExecuteChanged;

    public TResult? Result
    {
        get => _result;
    }

    public Exception? Error
    {
        get => _error;
    }

    public bool IsExecutable
    {
        get => _executeTask is null && _canExecute;
    }

    public bool IsExecuting
    {
        get => _executeTask is not null;
    }

    public void FireCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter)
    {
        var canExecuteResult = CanExecuteInternal(parameter);
        if (canExecuteResult != _canExecute)
        {
            _canExecute = canExecuteResult;
            OnPropertyChanged(nameof(IsExecutable));
        }
        return canExecuteResult;
    }

    public void Execute(object? parameter)
    {
        if (_executeTask is not null)
        {
            throw new InvalidOperationException("Execute task != null");
        }

        if (!AsyncCommandDelegate.CanExecute(CastParameter(parameter)))
        {
            throw new InvalidOperationException("CanExecute == false");
        }

        _executeTask = Task.Run(async () =>
        {
            _result = default;
            _error = null;

            DispatcherQueue.TryEnqueue(() => { FirePropertiesChanged(); });

            try
            {
                _result = await AsyncCommandDelegate.Execute(CastParameter(parameter));
                _error = null;
            }
            catch (Exception ex)
            {
                _result = default;
                _error = ex;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                _executeTask = null;
                FirePropertiesChanged();
            });
        });
    }

    bool CanExecuteInternal(object? parameter)
    {
        if (_executeTask is not null)
            return false;

        return AsyncCommandDelegate.CanExecute(CastParameter(parameter));
    }

    static Func<TParameter?, Task<TResult>> ToCallbackAsync(Func<TParameter?, TResult> executeCallback)
    {
        return parameter => { return Task.FromResult(executeCallback(parameter)); };
    }

    void FirePropertiesChanged()
    {
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(IsExecuting));
        OnPropertyChanged(nameof(IsExecutable));
        FireCanExecuteChanged();
    }

    static TParameter? CastParameter(object? parameter)
    {
        if (parameter is null)
            return default;

        return (TParameter)parameter;
    }
}


/// <summary>
///   Async command support with progress support
/// </summary>
internal partial class AsyncCommandWithProgress<TParameter, TResult, TProgress> : AsyncCommand<TParameter, TResult>
{
    private readonly AsyncCommand<object?, bool> _cancelCommand;

    private bool _isExecutingWithProgress;
    private IAsyncOperationWithProgress<TResult, TProgress>? _currentAsyncOperationWithProgress;
    private TProgress? _currentProgress;
    private TResult? _finalResult;

    internal AsyncCommandWithProgress(
        Func<TParameter, IAsyncOperationWithProgress<TResult, TProgress>> executeCallbackWithProgress,
        Func<TParameter?, bool> canExecuteCallback)
        : base(new DefaultAsyncCommandDelegate<TParameter, TResult>())
    {
        _cancelCommand = new(
            _ =>
            {
                (_currentAsyncOperationWithProgress ?? throw new InvalidOperationException("Not executing")).Cancel();
                return true;
            },
            _ => _isExecutingWithProgress);

        var asyncCommandDelegateInstance = (DefaultAsyncCommandDelegate<TParameter, TResult>)AsyncCommandDelegate;
        asyncCommandDelegateInstance.ExecuteHandler = async parameter =>
        {
            _currentAsyncOperationWithProgress = executeCallbackWithProgress(parameter!);
            _isExecutingWithProgress = true;

            await DispatcherQueue.EnqueueAsync(() => _cancelCommand.FireCanExecuteChanged());
            _currentAsyncOperationWithProgress.Progress = (_, progress) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    _currentProgress = progress;
                    OnPropertyChanged(nameof(CurrentProgress));
                    ResultProgressHandler?.Invoke(this, progress);
                });
            };

            try
            {
                _finalResult = await _currentAsyncOperationWithProgress;
                ResultHandler?.Invoke(this, _finalResult);
                return _finalResult;
            }
            finally
            {
                _currentAsyncOperationWithProgress = null;
                _isExecutingWithProgress = false;
                await DispatcherQueue.EnqueueAsync(() => _cancelCommand.FireCanExecuteChanged());
            }
        };
        asyncCommandDelegateInstance.CanExecuteHandler = canExecuteCallback;
    }

    public TProgress? CurrentProgress
    {
        get
        {
            return _currentProgress;
        }
    }

    public TResult? FinalResult
    {
        get
        {
            return _finalResult;
        }

    }

    public event EventHandler<TProgress>? ResultProgressHandler;
    public event EventHandler<TResult>? ResultHandler;

    public ICommand CancelCommand
    {
        get
        {
            return _cancelCommand;
        }
    }
}


internal class AsyncOperationWithProgress<TResult, TProgress> : IAsyncOperationWithProgress<TResult, TProgress>
{
    private static uint nextId;

    private AsyncOperationProgressHandler<TResult, TProgress>? _progress;
    private AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? _completed;

    private readonly TaskCompletionSource<TResult> _completionSource = new();
    private readonly CancellationTokenSource _cts = new();

    internal AsyncOperationWithProgress(Func<IProgress<TProgress>, CancellationToken, Task<TResult>> taskFactory)
    {
        Id = nextId++;
        var progressAdapter = new ProgressAdapter(progress =>
        {
            _progress?.Invoke(this, progress);
        });

        Task.Run(async () =>
        {
            try
            {
                var task = taskFactory(progressAdapter, _cts.Token);
                _completed?.Invoke(this, AsyncStatus.Started);
                var result = await task;
                _completionSource.SetResult(result);
                _completed?.Invoke(this, AsyncStatus.Completed);
            }
            catch (OperationCanceledException)
            {
                _completionSource.SetCanceled();
                _completed?.Invoke(this, AsyncStatus.Canceled);
            }
            catch (Exception ex)
            {
                _completionSource.SetException(ex);
                _completed?.Invoke(this, AsyncStatus.Error);
            }
            finally
            {
                _cts.Dispose();
            }
        });
    }

    public AsyncOperationProgressHandler<TResult, TProgress>? Progress
    {
        get => _progress;
        set => _progress = value;
    }

    public AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? Completed
    {
        get => _completed;
        set => _completed = value;
    }

    public Exception? ErrorCode
    {
        get => _completionSource.Task.Exception;
    }

    public uint Id { get; }

    public AsyncStatus Status
    {
        get
        {
            if (_cts.IsCancellationRequested)
            {
                return AsyncStatus.Canceled;
            }

            switch (_completionSource.Task.Status)
            {
                case TaskStatus.Faulted:
                    return AsyncStatus.Error;
                case TaskStatus.RanToCompletion:
                    return AsyncStatus.Completed;
                case TaskStatus.Created:
                    break;
                case TaskStatus.WaitingForActivation:
                    break;
                case TaskStatus.WaitingToRun:
                    break;
                case TaskStatus.Running:
                    break;
                case TaskStatus.WaitingForChildrenToComplete:
                    break;
                case TaskStatus.Canceled:
                    break;
            }

            return AsyncStatus.Started;
        }
    }

    public void Cancel()
    {
        _cts.Cancel();
    }

    public void Close()
    {
    }

    public TResult GetResults()
    {
        return _completionSource.Task.Result;
    }

    private sealed class ProgressAdapter : IProgress<TProgress>
    {
        private readonly Action<TProgress> _reportCallback;

        internal ProgressAdapter(Action<TProgress> reportCallback)
        {
            _reportCallback = reportCallback;
        }

        public void Report(TProgress value)
        {
            _reportCallback(value);
        }
    }
}


internal class AsyncOperationWithProgressAdapter<TResult, TProgress, TResultAdapter, TProgressAdapter> : IAsyncOperationWithProgress<TResultAdapter, TProgressAdapter>
{
    private readonly IAsyncOperationWithProgress<TResult, TProgress> _source;
    private AsyncOperationProgressHandler<TResultAdapter, TProgressAdapter>? _progress;
    private AsyncOperationWithProgressCompletedHandler<TResultAdapter, TProgressAdapter>? _completed;
    private readonly Func<TResult, TResultAdapter> _resultConverterCallback;
    private readonly Func<TProgress, TProgressAdapter> _progressConverterCallback;

    internal AsyncOperationWithProgressAdapter(
        IAsyncOperationWithProgress<TResult, TProgress> source,
        Func<TResult, TResultAdapter> resultConverterCallback,
        Func<TProgress, TProgressAdapter> progressConverterCallback)
    {
        _source = source;
        _resultConverterCallback = resultConverterCallback;
        _progressConverterCallback = progressConverterCallback;
    }

    public AsyncOperationProgressHandler<TResultAdapter, TProgressAdapter>? Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            void adapter(IAsyncOperationWithProgress<TResult, TProgress> _, TProgress progress)
            {
                _progress?.Invoke(this, _progressConverterCallback(progress));
            }
            _source.Progress = adapter;
        }
    }

    public AsyncOperationWithProgressCompletedHandler<TResultAdapter, TProgressAdapter>? Completed
    {
        get => _completed;
        set
        {
            _completed = value;
            void adapter(IAsyncOperationWithProgress<TResult, TProgress> _, AsyncStatus status)
            {
                // Note: the winRT async with progress brodge is only expecting 'real' completion status
                if (status != AsyncStatus.Started)
                {
                    _completed?.Invoke(this, status);
                }
            };
            _source.Completed = adapter;
        }
    }

    public Exception ErrorCode
    {
        get => _source.ErrorCode;
    }

    public uint Id
    {
        get => _source.Id;
    }

    public AsyncStatus Status
    {
        get => _source.Status;
    }

    public void Cancel() => _source.Cancel();

    public void Close() => _source.Close();

    public TResultAdapter GetResults() => _resultConverterCallback(_source.GetResults());
}


internal sealed class DefaultAsyncCommandDelegate<TParameter, TResult> : IAsyncCommandDelegate<TParameter, TResult>
{
    internal DefaultAsyncCommandDelegate()
    {
    }

    internal DefaultAsyncCommandDelegate(Func<TParameter?, Task<TResult>> executeCallback, Func<TParameter?, bool> canExecuteCallback)
    {
        ExecuteHandler = executeCallback;
        CanExecuteHandler = canExecuteCallback;
    }

    public Func<TParameter?, Task<TResult>>? ExecuteHandler { get; set; }
    public Func<TParameter?, bool>? CanExecuteHandler { get; set; }
    public bool CanExecute(TParameter? parameter) => (CanExecuteHandler ?? throw new InvalidOperationException("CanExecuteHandler == null"))(parameter);
    public Task<TResult> Execute(TParameter? parameter) => (ExecuteHandler ?? throw new InvalidOperationException("ExecuteHandler == null"))(parameter);
}
#endregion