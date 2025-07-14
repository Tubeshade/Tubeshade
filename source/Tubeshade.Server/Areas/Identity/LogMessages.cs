using System;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.Logging.LogLevel;

namespace Tubeshade.Server.Areas.Identity;

internal static partial class LogMessages
{
    [LoggerMessage(1, Information, "User created a new account with password")]
    internal static partial void UserCreated(this ILogger logger);

    [LoggerMessage(2, Information, "User created a new account using provider {LoginProvider}")]
    internal static partial void UserCreatedExternal(this ILogger logger, string loginProvider);

    [LoggerMessage(3, Information, "User logged in")]
    internal static partial void UserLoggedIn(this ILogger logger);

    [LoggerMessage(4, Warning, "User account locked out")]
    internal static partial void UserLockedOut(this ILogger logger);

    [LoggerMessage(5, Information, "User with ID '{UserId}' has enabled 2FA with an authenticator app")]
    internal static partial void UserEnabled2Fa(this ILogger logger, Guid userId);
}
