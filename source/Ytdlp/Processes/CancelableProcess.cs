using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ytdlp.Processes;

public sealed class CancelableProcess : IDisposable
{
    private readonly Process _process;

    private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _standardOutputCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _standardErrorCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<string> _standardOutput = new();
    private readonly ConcurrentQueue<string> _standardError = new();

    private bool _processStarted;

    public CancelableProcess(string fileName, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        _process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        _process.OutputDataReceived += ProcessOnOutputDataReceived;
        _process.ErrorDataReceived += ProcessOnErrorDataReceived;
        _process.Exited += ProcessOnExited;
    }

    public event ReceivedLineEventHandler? OutputReceived;

    public event ReceivedLineEventHandler? ErrorReceived;

    public IReadOnlyCollection<string> Output => _standardOutput;

    public IReadOnlyCollection<string> Error => _standardError;

    public async Task<int> Run(CancellationToken cancellationToken = default)
    {
        await using var registration = cancellationToken.Register(
            static state => ((CancelableProcess)state!).KillProcess(),
            this);

        if (!_process.Start())
        {
            // According to documentation, should only return false if process is already running
            throw new UnreachableException("Failed to start process");
        }

        _processStarted = true;

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        if (!_process.HasExited)
        {
            await _exited.Task;
        }

        await _standardOutputCompleted.Task;
        await _standardErrorCompleted.Task;

        return _process.ExitCode;
    }

    private void KillProcess()
    {
        if (!_processStarted || _process.HasExited)
        {
            return;
        }

        if (_process.TryTerminate())
        {
            return;
        }

        _process.Kill();
    }

    private void ProcessOnOutputDataReceived(object? sender, DataReceivedEventArgs args)
    {
        if (args.Data is null)
        {
            _standardOutputCompleted.SetResult();
            return;
        }

        _standardOutput.Enqueue(args.Data);
        OutputReceived?.Invoke(this, new(args.Data));
    }

    private void ProcessOnErrorDataReceived(object? sender, DataReceivedEventArgs args)
    {
        if (args.Data is null)
        {
            _standardErrorCompleted.SetResult();
            return;
        }

        _standardError.Enqueue(args.Data);
        ErrorReceived?.Invoke(this, new(args.Data));
    }

    private void ProcessOnExited(object? sender, EventArgs args) => _exited.TrySetResult();

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    ~CancelableProcess()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        KillProcess();
        if (!disposing)
        {
            return;
        }

        _process.OutputDataReceived -= ProcessOnOutputDataReceived;
        _process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        _process.Exited -= ProcessOnExited;

        _process.Dispose();
    }
}
