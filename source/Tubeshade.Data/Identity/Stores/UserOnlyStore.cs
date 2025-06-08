using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using NodaTime;
using Npgsql;
using Tubeshade.Data.Abstractions;
using Tubeshade.Data.AccessControl;

namespace Tubeshade.Data.Identity.Stores;

public sealed class UserOnlyStore :
    IUserLoginStore<UserEntity>,
    IUserClaimStore<UserEntity>,
    IUserPasswordStore<UserEntity>,
    IUserSecurityStampStore<UserEntity>,
    IUserEmailStore<UserEntity>,
    IUserLockoutStore<UserEntity>,
    IUserTwoFactorStore<UserEntity>,
    IUserAuthenticationTokenStore<UserEntity>,
    IUserAuthenticatorKeyStore<UserEntity>,
    IUserTwoFactorRecoveryCodeStore<UserEntity>
// IProtectedUserStore<UserEntity>
{
    private readonly NpgsqlConnection _connection;
    private readonly UserRepository _userRepository;
    private readonly OwnerRepository _ownerRepository;
    private readonly OwnershipRepository _ownershipRepository;
    private readonly IRepository<ClaimEntity> _claimRepository;
    private readonly UserLoginRepository _userLoginRepository;

    public UserOnlyStore(
        NpgsqlConnection connection,
        UserRepository userRepository,
        OwnerRepository ownerRepository,
        OwnershipRepository ownershipRepository,
        IRepository<ClaimEntity> claimRepository,
        UserLoginRepository userLoginRepository)
    {
        _userRepository = userRepository;
        _claimRepository = claimRepository;
        _userLoginRepository = userLoginRepository;
        _ownerRepository = ownerRepository;
        _ownershipRepository = ownershipRepository;
        _connection = connection;
    }

    /// <inheritdoc />
    public Task<string> GetUserIdAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    /// <inheritdoc />
    public Task<string?> GetUserNameAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Name);
    }

    /// <inheritdoc />
    public Task SetUserNameAsync(UserEntity user, string? userName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userName);
        user.Name = userName;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetNormalizedUserNameAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedName);
    }

    /// <inheritdoc />
    public Task SetNormalizedUserNameAsync(UserEntity user, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedName);
        user.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(UserEntity user, CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var userId = await _userRepository.AddAsync(user, transaction);
        if (userId is null)
        {
            return IdentityResult.Failed();
        }

        _ = await _ownerRepository.AddDefaultForUserAsync(userId.Value, transaction);
        _ = await _ownershipRepository.AddDefaultForUserAsync(userId.Value, transaction);

        await transaction.CommitAsync(cancellationToken);
        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(UserEntity user, CancellationToken cancellationToken)
    {
        user.ConcurrencyStamp = Guid.NewGuid();

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var rows = await _userRepository.UpdateAsync(user, transaction);
        await transaction.CommitAsync(cancellationToken);
        return rows is 1 ? IdentityResult.Success : IdentityResult.Failed(new IdentityError());
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAsync(UserEntity user, CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var rows = await _userRepository.DeleteAsync(user, transaction);
        await transaction.CommitAsync(cancellationToken);
        return rows is 1 ? IdentityResult.Success : IdentityResult.Failed(new IdentityError());
    }

    /// <inheritdoc />
    public async Task<UserEntity?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _userRepository.FindAsync(Guid.Parse(userId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserEntity?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await _userRepository.FindAsync(normalizedUserName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddLoginAsync(UserEntity user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        var userLogin = new UserLogin
        {
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName,
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
        };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var rows = await _userLoginRepository.AddAsync(userLogin, transaction);
        if (rows is not 1)
        {
            throw new();
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveLoginAsync(
        UserEntity user,
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var userLogin = new UserLogin
        {
            ProviderKey = providerKey,
            UserId = user.Id,
            LoginProvider = loginProvider,
        };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var rows = await _userLoginRepository.DeleteAsync(userLogin, transaction);
        if (rows is not 1)
        {
            throw new();
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<UserLoginInfo>> GetLoginsAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var userLogins = await _userLoginRepository.GetAsync(user.Id, cancellationToken);
        return userLogins
            .Select(login => new UserLoginInfo(login.LoginProvider, login.ProviderKey, login.ProviderDisplayName))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<UserEntity?> FindByLoginAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        return await _userRepository.FindByLoginAsync(loginProvider, providerKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<Claim>> GetClaimsAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var claims = await _claimRepository.GetAsync(user.Id, cancellationToken);
        return claims.Select(entity => entity.ToClaim()).ToList();
    }

    /// <inheritdoc />
    public Task AddClaimsAsync(UserEntity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task ReplaceClaimAsync(UserEntity user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveClaimsAsync(UserEntity user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<IList<UserEntity>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task SetPasswordHashAsync(UserEntity user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash is null ? null : Convert.FromBase64String(passwordHash);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetPasswordHashAsync(UserEntity user, CancellationToken cancellationToken)
    {
        if (user.PasswordHash is not { } hash)
        {
            return Task.FromResult<string?>(null);
        }

        var base64 = Convert.ToBase64String(hash);
        return Task.FromResult<string?>(base64);
    }

    /// <inheritdoc />
    public Task<bool> HasPasswordAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash is not null);
    }

    /// <inheritdoc />
    public Task SetSecurityStampAsync(UserEntity user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = Guid.TryParse(stamp, out var guid) ? guid : Guid.NewGuid();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetSecurityStampAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp?.ToString());
    }

    /// <inheritdoc />
    public Task SetEmailAsync(UserEntity user, string? email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        user.Email = email;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetEmailAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    /// <inheritdoc />
    public Task<bool> GetEmailConfirmedAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    /// <inheritdoc />
    public Task SetEmailConfirmedAsync(UserEntity user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<UserEntity?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _userRepository.FindByEmailAsync(normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string?> GetNormalizedEmailAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedEmail);
    }

    /// <inheritdoc />
    public Task SetNormalizedEmailAsync(UserEntity user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedEmail);
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd?.ToDateTimeOffset());
    }

    /// <inheritdoc />
    public Task SetLockoutEndDateAsync(UserEntity user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd is null ? null : Instant.FromDateTimeOffset(lockoutEnd.Value);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> IncrementAccessFailedCountAsync(UserEntity user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    /// <inheritdoc />
    public Task ResetAccessFailedCountAsync(UserEntity user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> GetAccessFailedCountAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    /// <inheritdoc />
    public Task<bool> GetLockoutEnabledAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    /// <inheritdoc />
    public Task SetLockoutEnabledAsync(UserEntity user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetTwoFactorEnabledAsync(UserEntity user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> GetTwoFactorEnabledAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    /// <inheritdoc />
    public Task SetTokenAsync(UserEntity user, string loginProvider, string name, string? value,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveTokenAsync(UserEntity user, string loginProvider, string name,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<string?> GetTokenAsync(UserEntity user, string loginProvider, string name,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task SetAuthenticatorKeyAsync(UserEntity user, string key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<string?> GetAuthenticatorKeyAsync(UserEntity user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task ReplaceCodesAsync(UserEntity user, IEnumerable<string> recoveryCodes,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> RedeemCodeAsync(UserEntity user, string code, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<int> CountCodesAsync(UserEntity user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
