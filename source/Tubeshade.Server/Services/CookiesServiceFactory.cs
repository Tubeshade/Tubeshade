using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Services;

public sealed class CookiesServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LibraryCookieRepository _repository;

    public CookiesServiceFactory(ILoggerFactory loggerFactory, LibraryCookieRepository repository)
    {
        _loggerFactory = loggerFactory;
        _repository = repository;
    }

    public CookiesService Create(
        Guid userId,
        Guid libraryId,
        DirectoryInfo directory,
        CancellationToken cancellationToken)
    {
        return new(
            _loggerFactory.CreateLogger<CookiesService>(),
            _repository,
            userId,
            libraryId,
            directory,
            cancellationToken);
    }
}
