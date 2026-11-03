using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ytdlp.Processes;

public sealed class CancelableProcess : IAsyncDisposable
{
    private readonly bool _standardOutputEvents;
    private readonly bool _standardErrorEvents;
    private readonly Process _process;
    private readonly CancellationTokenSource _disposed = new();

    private readonly TaskCompletionSource _standardOutputCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _standardErrorCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<string> _standardOutputLines = new();
    private readonly ConcurrentQueue<string> _standardErrorLines = new();

    private Task<int>? _runTask;

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
    }

    public event ReceivedLineEventHandler? OutputReceived;

    public event ReceivedLineEventHandler? ErrorReceived;

    public string FileName => _process.StartInfo.FileName;

    public IReadOnlyCollection<string> OutputLines => _standardOutputLines;

    public IReadOnlyCollection<string> ErrorLines => _standardErrorLines;

    public Stream Input => _process.StandardInput.BaseStream;

    public Stream Output => _process.StandardOutput.BaseStream;

    public Stream Error => _process.StandardError.BaseStream;

    public Task<int> Run(CancellationToken cancellationToken = default)
    {
        return _runTask ??= RunProcess(cancellationToken);
    }

    private async Task<int> RunProcess(CancellationToken cancellationToken)
    {
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposed.Token);

        if (!_process.Start())
        {
            // According to documentation, should only return false if process is already running
            throw new UnreachableException("Failed to start process");
        }

        if (_standardOutputEvents)
        {
            _process.BeginOutputReadLine();
        }

        if (_standardErrorEvents)
        {
            _process.BeginErrorReadLine();
        }

        try
        {
            await _process.WaitForExitAsync(stopping.Token);
        }
        catch (OperationCanceledException)
        {
            await Stop();
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

    private async Task Stop()
    {
        _ = _process.TryTerminate();

        using var timeout = new CancellationTokenSource(500);

        try
        {
            await _process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(CancellationToken.None);
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _disposed.CancelAsync();

        if (_runTask is not null)
        {
            try
            {
                await _runTask;
            }
            catch (Exception)
            {
                // Exceptions are observed by whoever awaits the task returned by Run
            }
        }

        if (_standardOutputEvents)
        {
            _process.OutputDataReceived -= ProcessOnOutputDataReceived;
        }

        if (_standardErrorEvents)
        {
            _process.ErrorDataReceived -= ProcessOnErrorDataReceived;
        }

        _process.Dispose();
        _disposed.Dispose();
    }
}
