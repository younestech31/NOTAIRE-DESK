using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotaireDesk.Api.Security;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;

namespace NotaireDesk.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users").RequireAuthorization();
        users.MapGet("/", ListUsersAsync).RequireAuthorization(PermissionCodes.UsersRead);
        users.MapPost("/", CreateUserAsync).RequireAuthorization(PermissionCodes.UsersManage);
        users.MapPut("/{id:guid}/roles", ReplaceRolesAsync).RequireAuthorization(PermissionCodes.UsersManage);
        users.MapPut("/{id:guid}/disable", DisableUserAsync).RequireAuthorization(PermissionCodes.UsersManage);
        users.MapPut("/{id:guid}/password", ResetPasswordAsync).RequireAuthorization(PermissionCodes.UsersManage);

        var roles = endpoints.MapGroup("/api/roles").RequireAuthorization();
        roles.MapGet("/", ListRolesAsync).RequireAuthorization(PermissionCodes.RolesRead);
        roles.MapPost("/", CreateRoleAsync).RequireAuthorization(PermissionCodes.RolesManage);
        roles.MapPut("/{id:guid}/permissions", ReplaceRolePermissionsAsync)
            .RequireAuthorization(PermissionCodes.RolesManage);
        return endpoints;
    }

    private static async Task<IResult> ListUsersAsync(OfficeDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(x => x.NormalizedUsername)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.DisplayNameAr,
                x.DisplayNameFr,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(x => x.Id).ToArray();
        var roles = await (
            from userRole in db.Set<OfficeUserRole>().AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Code, role.NameAr, role.NameFr }
        ).ToListAsync(cancellationToken);

        return Results.Ok(users.Select(user => new
        {
            user.Id,
            user.Username,
            user.DisplayNameAr,
            user.DisplayNameFr,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            Roles = roles.Where(x => x.UserId == user.Id)
                .Select(x => new { x.Code, x.NameAr, x.NameFr })
                .ToArray()
        }));
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        OfficeDbContext db,
        IPasswordHasher<OfficeUser> passwordHasher,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(request.InitialPassword))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["initialPassword"] = [$"Password must be {PasswordPolicy.MinimumLength} to {PasswordPolicy.MaximumLength} characters."]
            });

        if (!AreValidIds(request.RoleIds))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["Assign at least one unique, valid role."]
            });

        var username = request.Username?.Trim();
        var nameAr = request.DisplayNameAr?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 80 ||
            username.Any(char.IsWhiteSpace) ||
            string.IsNullOrWhiteSpace(nameAr) || nameAr.Length > 200 ||
            request.DisplayNameFr?.Length > 200)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["user"] = ["Username or display names are invalid."]
            });

        var normalized = username.ToUpperInvariant();
        if (await db.Users.AnyAsync(x => x.NormalizedUsername == normalized, cancellationToken))
            return Results.Conflict(new { error = "username_already_exists" });

        var roles = await db.Roles.Where(x => request.RoleIds!.Contains(x.Id)).ToListAsync(cancellationToken);
        if (roles.Count != request.RoleIds!.Length)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["One or more roles do not exist."]
            });

        var now = DateTimeOffset.UtcNow;
        var user = OfficeUser.Create(username, nameAr, request.DisplayNameFr, "placeholder", now);
        user.ReplacePasswordHash(passwordHasher.HashPassword(user, request.InitialPassword!), now);
        user.ReplaceRoles(request.RoleIds!, DateTimeOffset.UtcNow);

        db.Users.Add(user);
        audit.Add("users.created", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Succeeded,
            $"role_count:{request.RoleIds!.Length}");
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/users/{user.Id:D}", new
        {
            user.Id,
            user.Username,
            user.DisplayNameAr,
            user.DisplayNameFr,
            IsActive = user.IsActive
        });
    }

    private static async Task<IResult> ReplaceRolesAsync(
        Guid id,
        ReplaceRolesRequest request,
        OfficeDbContext db,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!AreValidIds(request.RoleIds))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["Assign at least one unique, valid role."]
            });

        var user = await db.Users.Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Results.NotFound();

        var roles = await db.Roles.Where(x => request.RoleIds!.Contains(x.Id))
            .ToListAsync(cancellationToken);
        if (roles.Count != request.RoleIds!.Length)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["One or more roles do not exist."]
            });

        var currentHasAdmin = await (
            from userRole in db.Set<OfficeUserRole>()
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id && role.Code == SystemRoleCodes.OfficeAdministrator
            select userRole
        ).AnyAsync(cancellationToken);
        var targetKeepsAdmin = roles.Any(x => x.Code == SystemRoleCodes.OfficeAdministrator);
        if (user.IsActive && currentHasAdmin && !targetKeepsAdmin)
        {
            var otherActiveAdminExists = await HasAnotherActiveAdministratorAsync(db, user.Id, cancellationToken);
            if (!otherActiveAdminExists)
                return Results.Conflict(new { error = "cannot_remove_last_office_administrator" });
        }

        user.ReplaceRoles(request.RoleIds!);
        await RevokeAllSessionsAsync(db, user.Id, DateTimeOffset.UtcNow, cancellationToken);
        audit.Add("users.roles.replaced", "OfficeUser", user.Id.ToString("D"),
            AuditOutcome.Succeeded, $"role_count:{request.RoleIds!.Length}");
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DisableUserAsync(
        Guid id,
        ClaimsPrincipal principal,
        OfficeDbContext db,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Results.Unauthorized();
        if (actorId == id)
            return Results.Conflict(new { error = "cannot_disable_current_user" });

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Results.NotFound();
        if (!user.IsActive)
            return Results.NoContent();

        var isAdmin = await (
            from userRole in db.Set<OfficeUserRole>()
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == id && role.Code == SystemRoleCodes.OfficeAdministrator
            select userRole
        ).AnyAsync(cancellationToken);

        if (isAdmin && !await HasAnotherActiveAdministratorAsync(db, id, cancellationToken))
            return Results.Conflict(new { error = "cannot_disable_last_office_administrator" });

        var now = DateTimeOffset.UtcNow;
        user.Disable(now);
        await RevokeAllSessionsAsync(db, user.Id, now, cancellationToken);
        audit.Add("users.disabled", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Succeeded);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        OfficeDbContext db,
        IPasswordHasher<OfficeUser> passwordHasher,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["newPassword"] = [$"Password must be {PasswordPolicy.MinimumLength} to {PasswordPolicy.MaximumLength} characters."]
            });

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Results.NotFound();

        var now = DateTimeOffset.UtcNow;
        user.ReplacePasswordHash(passwordHasher.HashPassword(user, request.NewPassword), now);
        await RevokeAllSessionsAsync(db, user.Id, now, cancellationToken);
        audit.Add("users.password.reset", "OfficeUser", user.Id.ToString("D"), AuditOutcome.Succeeded);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListRolesAsync(OfficeDbContext db, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.NameAr,
                x.NameFr,
                x.IsSystemRole,
                Permissions = x.Permissions.Select(p => p.Permission).OrderBy(p => p).ToArray()
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleRequest request,
        OfficeDbContext db,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NameAr) ||
            request.Code.Length > 50 ||
            request.NameAr.Length > 150 ||
            request.NameFr?.Length > 150 ||
            request.Permissions is null ||
            request.Permissions.Length == 0 ||
            request.Permissions.Distinct(StringComparer.Ordinal).Count() != request.Permissions.Length ||
            request.Permissions.Any(x => !PermissionCodes.All.Contains(x)))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = ["Role code, names or permission list is invalid."]
            });

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(x => x.Code == code, cancellationToken))
            return Results.Conflict(new { error = "role_code_already_exists" });

        var role = OfficeRole.Create(code, request.NameAr, request.NameFr, false, DateTimeOffset.UtcNow);
        foreach (var permission in request.Permissions)
            role.AddPermission(permission);

        db.Roles.Add(role);
        audit.Add("roles.created", "OfficeRole", role.Id.ToString("D"),
            AuditOutcome.Succeeded, $"permission_count:{request.Permissions.Length}");
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/roles/{role.Id:D}", new { role.Id, role.Code, role.NameAr, role.NameFr });
    }

    private static async Task<IResult> ReplaceRolePermissionsAsync(
        Guid id,
        ReplaceRolePermissionsRequest request,
        OfficeDbContext db,
        SecurityAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (request.Permissions is null ||
            request.Permissions.Length == 0 ||
            request.Permissions.Distinct(StringComparer.Ordinal).Count() != request.Permissions.Length ||
            request.Permissions.Any(x => !PermissionCodes.All.Contains(x)))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["permissions"] = ["Provide at least one unique, known permission."]
            });

        var role = await db.Roles.Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
            return Results.NotFound();
        if (role.IsSystemRole)
            return Results.Conflict(new { error = "system_roles_are_managed_by_the_application" });

        var requested = request.Permissions.ToHashSet(StringComparer.Ordinal);
        foreach (var existing in role.Permissions.Select(x => x.Permission).ToArray())
        {
            if (!requested.Contains(existing))
                role.RemovePermission(existing);
        }
        foreach (var permission in requested)
            role.AddPermission(permission);

        var assignedUserIds = await db.Set<OfficeUserRole>().AsNoTracking()
            .Where(x => x.RoleId == id)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
        await RevokeSessionsForUsersAsync(db, assignedUserIds, DateTimeOffset.UtcNow, cancellationToken);

        audit.Add("roles.permissions.replaced", "OfficeRole", role.Id.ToString("D"),
            AuditOutcome.Succeeded, $"permission_count:{requested.Count}");
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<bool> HasAnotherActiveAdministratorAsync(
        OfficeDbContext db,
        Guid excludedUserId,
        CancellationToken cancellationToken) =>
        await (
            from userRole in db.Set<OfficeUserRole>()
            join role in db.Roles on userRole.RoleId equals role.Id
            join otherUser in db.Users on userRole.UserId equals otherUser.Id
            where role.Code == SystemRoleCodes.OfficeAdministrator &&
                  otherUser.IsActive && otherUser.Id != excludedUserId
            select otherUser.Id
        ).AnyAsync(cancellationToken);

    private static async Task RevokeAllSessionsAsync(
        OfficeDbContext db,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sessions = await db.UserSessions
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.Revoke(now);
    }

    private static async Task RevokeSessionsForUsersAsync(
        OfficeDbContext db,
        IReadOnlyCollection<Guid> userIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return;
        var sessions = await db.UserSessions
            .Where(x => userIds.Contains(x.UserId) && x.RevokedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.Revoke(now);
    }

    private static bool AreValidIds(Guid[]? ids) =>
        ids is { Length: > 0 } &&
        ids.All(x => x != Guid.Empty) &&
        ids.Distinct().Count() == ids.Length;
}

public sealed record CreateUserRequest(
    string? Username,
    string? DisplayNameAr,
    string? DisplayNameFr,
    string? InitialPassword,
    Guid[]? RoleIds);

public sealed record ReplaceRolesRequest(Guid[]? RoleIds);
public sealed record ResetPasswordRequest(string NewPassword);
public sealed record CreateRoleRequest(string Code, string NameAr, string? NameFr, string[]? Permissions);
public sealed record ReplaceRolePermissionsRequest(string[]? Permissions);
