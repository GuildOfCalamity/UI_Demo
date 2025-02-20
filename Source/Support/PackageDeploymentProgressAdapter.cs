using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Windows.Management.Deployment;

namespace UI_Demo;

sealed class PackageDeploymentProgressAdapter : IProgress<PackageDeploymentProgress>, IAsyncDisposable
{
    static readonly TimeSpan progressTimeout = TimeSpan.FromMilliseconds(2500);
    const double ProgressIncrement = 5.0;
    readonly IProgress<PackageDeploymentProgress> _progress;
    readonly CancellationTokenSource _disposeCts = new();
    CancellationTokenSource _reportCts;
    PackageDeploymentProgress _lastProgress;
    readonly Task _runProgressTask;

    CancellationToken DisposeToken => _disposeCts.Token;

    internal PackageDeploymentProgressAdapter(IProgress<PackageDeploymentProgress> progress)
    {
        _progress = progress;
        _reportCts = CancellationTokenSource.CreateLinkedTokenSource(DisposeToken);
        _runProgressTask = Task.Run(RunProgressAsync, DisposeToken);
    }


    public void Report(PackageDeploymentProgress value)
    {
        _lastProgress = value;
        _reportCts.Cancel();
        _progress.Report(value);
    }

    async Task RunProgressAsync()
    {
        while (!DisposeToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(progressTimeout, _reportCts.Token);
                // no report was found
                var nextProgressValue = (_lastProgress.Progress + ProgressIncrement) % 95.0;
                _lastProgress = new PackageDeploymentProgress(PackageDeploymentProgressStatus.InProgress, nextProgressValue);
                _progress.Report(_lastProgress);
            }
            catch (OperationCanceledException)
            {
                _reportCts = CancellationTokenSource.CreateLinkedTokenSource(DisposeToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _progress.Report(new PackageDeploymentProgress(PackageDeploymentProgressStatus.CompletedSuccess, 100.0));
        _disposeCts.Cancel();
        try { await _runProgressTask; }
        catch (OperationCanceledException) { }
        _disposeCts.Dispose();
    }
}
