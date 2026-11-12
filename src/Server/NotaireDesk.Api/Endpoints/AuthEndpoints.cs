using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotaireDesk.Api.Security;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;

namespace NotaireDesk.Api.Endpoints;

public static class AuthEndpoints
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaximumFailedAttempts = 5;

    private static readonly OfficeUser DummyUser = OfficeUser.Create(
        "authentication-dummy",
        "Authentication Dummy",
        null,
        "dummy-placeholder",
        DateTimeOffset.UnixEpoch);

    private static readonly string DummyPasswordHash = new PasswordHasher<OfficeUser>(
        Options.Create(new PasswordHasherOptions { IterationCount = 310_000 }))
        .HashPassword(DummyUser, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("login");
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous().RequireRateLimiting("login");
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", MeAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        OfficeDbContext db,
        IPasswordHasher<OfficeUser> passwordHasher,
        AccessTokenIssuer tokenIssuer,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            request.Username.Length > 80 ||
            string.IsNullOrEmpty(request.Password) ||
            request.Password.Length > PasswordPolicy.MaximumLength)
        {
            passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, "constant-time-dummy-check");
            audit.Add("auth.login.failed", "Authentication", null, AuditOutcome.Failed, "invalid_credentials",
                actorUserId: null, actorUsername: null);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var normalized = request.Username.Trim().ToUpperInvariant();
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.NormalizedUsername == normalized, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, request.Password);
            audit.Add("auth.login.failed", "Authentication", null, AuditOutcome.Failed, "invalid_credentials",
                actorUserId: null, actorUsername: null);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        if (!user.IsActive || user.IsLockedOut(now))
        {
            passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, request.Password);
            audit.Add("auth.login.failed", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Denied,
                user.IsActive ? "account_locked" : "account_disabled",
                actorUserId: null, actorUsername: null);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin(now, MaximumFailedAttempts, LockoutDuration);
            audit.Add("auth.login.failed", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Failed,
                "invalid_credentials", actorUserId: null, actorUsername: null);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Unauthorized();
        }

        user.RecordSuccessfulLogin(now);
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.ReplacePasswordHash(passwordHasher.HashPassword(user, request.Password), now);

        var roleData = await LoadRoleDataAsync(db, user.Id, cancellationToken);
        if (roleData.RoleCodes.Count == 0)
        {
            audit.Add("auth.login.denied", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Denied,
                "no_roles_assigned", user.Id, user.Username);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Forbid();
        }

        var refreshToken = RefreshTokenFactory.Create();
        var session = UserSession.Create(
            user.Id,
            RefreshTokenFactory.Hash(refreshToken),
            now,
            now.Add(RefreshLifetime),
            NormalizeDeviceLabel(request.DeviceLabel));

        db.UserSessions.Add(session);
        audit.Add("auth.login.succeeded", "UserSession", session.Id.ToString("D"),
            AuditOutcome.Succeeded, null, user.Id, user.Username);
        await db.SaveChangesAsync(cancellationToken);

        var accessToken = tokenIssuer.Create(user, session.Id, roleData.RoleCodes, roleData.Permissions);
        return Results.Ok(new TokenResponse(
            accessToken,
            refreshToken,
            AccessTokenIssuer.AccessTokenLifetime.TotalSeconds,
            session.ExpiresAt));
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        OfficeDbContext db,
        AccessTokenIssuer tokenIssuer,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken) ||
            request.RefreshToken.Length is < 50 or > 120)
            return Results.Unauthorized();

        var tokenHash = RefreshTokenFactory.Hash(request.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var session = await db.UserSessions
            .SingleOrDefaultAsync(x => x.RefreshTokenHash == tokenHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (session is null || !session.IsActive(now))
        {
            audit.Add("auth.refresh.failed", "UserSession", null, AuditOutcome.Denied, "invalid_refresh_token");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            session.Revoke(now);
            audit.Add("auth.refresh.denied", "UserSession", session.Id.ToString("D"), AuditOutcome.Denied,
                "account_unavailable");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var roleData = await LoadRoleDataAsync(db, user.Id, cancellationToken);
        if (roleData.RoleCodes.Count == 0)
        {
            session.Revoke(now);
            audit.Add("auth.refresh.denied", "UserSession", session.Id.ToString("D"), AuditOutcome.Denied,
                "no_roles_assigned", user.Id, user.Username);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var nextRefreshToken = RefreshTokenFactory.Create();
        var nextSession = UserSession.Create(
            user.Id,
            RefreshTokenFactory.Hash(nextRefreshToken),
            now,
            now.Add(RefreshLifetime),
            session.DeviceLabel);
        session.Revoke(now, nextSession.Id);
        db.UserSessions.Add(nextSession);

        audit.Add("auth.refresh.succeeded", "UserSession", nextSession.Id.ToString("D"),
            AuditOutcome.Succeeded, null, user.Id, user.Username);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Unauthorized();
        }

        var accessToken = tokenIssuer.Create(user, nextSession.Id, roleData.RoleCodes, roleData.Permissions);
        return Results.Ok(new TokenResponse(
            accessToken,
            nextRefreshToken,
            AccessTokenIssuer.AccessTokenLifetime.TotalSeconds,
            nextSession.ExpiresAt));
    }

    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal principal,
        OfficeDbContext db,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Jti), out var sessionId))
            return Results.Unauthorized();

        var session = await db.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return Results.NoContent();

        if (session.IsActive(DateTimeOffset.UtcNow))
        {
            session.Revoke(DateTimeOffset.UtcNow);
            audit.Add("auth.logout", "UserSession", session.Id.ToString("D"), AuditOutcome.Succeeded);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> MeAsync(
        ClaimsPrincipal principal,
        OfficeDbContext db,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Results.Unauthorized();

        var user = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.DisplayNameAr,
                x.DisplayNameFr,
                x.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Results.Unauthorized() : Results.Ok(user);
    }

    internal static async Task<(List<string> RoleCodes, List<string> Permissions)> LoadRoleDataAsync(
        OfficeDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var roleData = await (
            from userRole in db.Set<OfficeUserRole>().AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select new { role.Code, role.Id }
        ).ToListAsync(cancellationToken);

        var roleIds = roleData.Select(x => x.Id).ToArray();
        var permissions = await db.Set<OfficeRolePermission>().AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roleData.Select(x => x.Code).Distinct(StringComparer.Ordinal).ToList(),
            permissions.Distinct(StringComparer.Ordinal).ToList());
    }

    private static string? NormalizeDeviceLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var label = value.Trim();
        return label.Length <= 120 ? label : label[..120];
    }
}

public sealed record LoginRequest(string Username, string Password, string? DeviceLabel);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    double ExpiresInSeconds,
    DateTimeOffset RefreshExpiresAt);
