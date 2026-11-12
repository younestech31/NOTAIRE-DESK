using Microsoft.EntityFrameworkCore;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;

namespace NotaireDesk.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/audit", ReadAsync)
            .RequireAuthorization(PermissionCodes.AuditRead);
        return endpoints;
    }

    private static async Task<IResult> ReadAsync(
        OfficeDbContext db,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? take,
        string? action,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(take ?? 100, 1, 200);
        if (from.HasValue && to.HasValue && from > to)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dateRange"] = ["The from date must not be after the to date."]
            });

        var query = db.AuditEvents.AsNoTracking().AsQueryable();
        if (from.HasValue)
            query = query.Where(x => x.OccurredAt >= from.Value.ToUniversalTime());
        if (to.HasValue)
            query = query.Where(x => x.OccurredAt <= to.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action.Trim());

        var events = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.OccurredAt,
                x.ActorUserId,
                x.ActorUsername,
                x.Action,
                x.TargetType,
                x.TargetId,
                x.Outcome,
                x.ClientAddress,
                x.CorrelationId,
                x.SafeMetadata
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(events);
    }
}
