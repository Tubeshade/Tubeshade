using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Server.Configuration;

namespace Tubeshade.Server.Services;

public sealed class FileSystemService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<YtdlpOptions> _options;

    public FileSystemService(ILoggerFactory loggerFactory, IOptionsMonitor<YtdlpOptions> options)
    {
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public ScopedDirectory CreateTemporaryDirectory(string prefix, Guid id)
    {
        return CreateTemporaryDirectory(prefix, id.ToString("N", CultureInfo.InvariantCulture));
    }

    public ScopedDirectory CreateTemporaryDirectory(string prefix, string name)
    {
        var logger = _loggerFactory.CreateLogger<ScopedDirectory>();

        var rootDirectory = new DirectoryInfo(_options.CurrentValue.TempPath);
        var directory = rootDirectory.CreateSubdirectory($"ts_{prefix}_{name}");

        return new ScopedDirectory(logger, directory);
    }

    /// <summary>A directory that is automatically deleted.</summary>
    public readonly struct ScopedDirectory : IDisposable
    {
        private readonly ILogger _logger;

        public ScopedDirectory(ILogger logger, DirectoryInfo directory)
        {
            Directory = directory;
            _logger = logger;

            _logger.Created(Directory.FullName);
        }

        public DirectoryInfo Directory { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            _logger.Deleting(Directory.FullName);
            Directory.Delete(true);
        }
    }
}
