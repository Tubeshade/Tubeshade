using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Services;

public sealed class CookiesService
{
    private const string CookieFileName = "cookie.jar";

    private readonly ILogger<CookiesService> _logger;
    private readonly LibraryCookieRepository _repository;
    private readonly Guid _userId;
    private readonly Guid _libraryId;
    private readonly DirectoryInfo _directory;
    private readonly CancellationToken _cancellationToken;

    public CookiesService(
        ILogger<CookiesService> logger,
        LibraryCookieRepository repository,
        Guid userId,
        Guid libraryId,
        DirectoryInfo directory,
        CancellationToken cancellationToken)
    {
        _repository = repository;
        _userId = userId;
        _libraryId = libraryId;
        _directory = directory;
        _cancellationToken = cancellationToken;
        _logger = logger;
    }

    public async ValueTask<string?> RefreshCookieFile()
    {
        const string domain = "youtube.com";

        var cookie = await _repository.FindByDomain(domain, _userId, _libraryId, Access.Read, _cancellationToken);
        if (cookie?.Cookie is null)
        {
            _logger.NoCookies(domain);
            return null;
        }

        var cookieFilepath = Path.Combine(_directory.FullName, CookieFileName);
        _logger.WritingCookies(domain, cookieFilepath);

        await File.WriteAllTextAsync(cookieFilepath, cookie.Cookie, _cancellationToken);

        return cookieFilepath;
    }
}
