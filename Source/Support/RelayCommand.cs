using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UI_Demo;

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
