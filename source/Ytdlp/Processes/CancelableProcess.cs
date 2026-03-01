using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ytdlp.Processes;

public sealed class CancelableProcess : IDisposable
{
    private const int GracefulShutdownTimeout = 500;

    private readonly bool _standardOutputEvents;
    private readonly bool _standardErrorEvents;
    private readonly Process _process;

    private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _standardOutputCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _standardErrorCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<string> _standardOutputLines = new();
    private readonly ConcurrentQueue<string> _standardErrorLines = new();

    private bool _processStarted;

    public CancelableProcess(
        string fileName,
        IEnumerable<string> arguments,
        bool standardOutputEvents = true,
        bool standardErrorEvents = true)
        : this(fileName, string.Join(' ', arguments), standardOutputEvents, standardErrorEvents)
    {
    }

    public CancelableProcess(
        string fileName,
        string arguments,
        bool standardOutputEvents = true,
        bool standardErrorEvents = true)
    {
        _standardOutputEvents = standardOutputEvents;
        _standardErrorEvents = standardErrorEvents;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        _process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true,
        };

        if (_standardOutputEvents)
        {
            _process.OutputDataReceived += ProcessOnOutputDataReceived;
        }

        if (_standardErrorEvents)
        {
            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
        }

        _process.Exited += ProcessOnExited;
    }

    public event ReceivedLineEventHandler? OutputReceived;

    public event ReceivedLineEventHandler? ErrorReceived;

    public string FileName => _process.StartInfo.FileName;

    public IReadOnlyCollection<string> OutputLines => _standardOutputLines;

    public IReadOnlyCollection<string> ErrorLines => _standardErrorLines;

    public Stream Input => _process.StandardInput.BaseStream;

    public Stream Output => _process.StandardOutput.BaseStream;

    public Stream Error => _process.StandardError.BaseStream;

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

        if (_standardOutputEvents)
        {
            _process.BeginOutputReadLine();
        }

        if (_standardErrorEvents)
        {
            _process.BeginErrorReadLine();
        }

        if (!_process.HasExited)
        {
            await _exited.Task;
        }

        if (_standardOutputEvents)
        {
            await _standardOutputCompleted.Task;
        }

        if (_standardErrorEvents)
        {
            await _standardErrorCompleted.Task;
        }

        return _process.ExitCode;
    }

    private void KillProcess()
    {
        if (_processStarted && !_process.HasExited)
        {
            _ = _process.TryTerminate();
            Thread.Sleep(GracefulShutdownTimeout);
        }

        if (_processStarted)
        {
            _process.Kill();
        }
    }

    private void ProcessOnOutputDataReceived(object? sender, DataReceivedEventArgs args)
    {
        if (args.Data is null)
        {
            _standardOutputCompleted.SetResult();
            return;
        }

        _standardOutputLines.Enqueue(args.Data);
        OutputReceived?.Invoke(this, new(args.Data));
    }

    private void ProcessOnErrorDataReceived(object? sender, DataReceivedEventArgs args)
    {
        if (args.Data is null)
        {
            _standardErrorCompleted.SetResult();
            return;
        }

        _standardErrorLines.Enqueue(args.Data);
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

        if (_standardOutputEvents)
        {
            _process.OutputDataReceived -= ProcessOnOutputDataReceived;
        }

        if (_standardErrorEvents)
        {
            _process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        _process.Exited -= ProcessOnExited;

        _process.Dispose();
    }
}
