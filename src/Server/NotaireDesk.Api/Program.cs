using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotaireDesk.Api.Endpoints;
using NotaireDesk.Api.Security;
using NotaireDesk.Domain.Security;
using NotaireDesk.Infrastructure.Persistence;
using NotaireDesk.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OfficeDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Missing ConnectionStrings:OfficeDatabase. Configure the PostgreSQL connection in trusted local server configuration.");

var jwtOptions = JwtOptions.Load(builder.Configuration);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<AccessTokenIssuer>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<OfficeDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(OfficeDbContext).Assembly.FullName)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SecurityAuditWriter>();
builder.Services.AddScoped<OfficeSecuritySeeder>();
builder.Services.AddScoped<IPasswordHasher<OfficeUser>, PasswordHasher<OfficeUser>>();
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 310_000);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtOptions.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        // A valid JWT is not enough: the persisted session and office account
        // must still be active, so logout, password reset and role changes take effect.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (!Guid.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Jti), out var sessionId) ||
                    !Guid.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                {
                    context.Fail("Invalid session claims.");
                    return;
                }

                var database = context.HttpContext.RequestServices.GetRequiredService<OfficeDbContext>();
                var now = DateTimeOffset.UtcNow;
                var session = await database.UserSessions.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == sessionId, context.HttpContext.RequestAborted);

                if (session is null || session.UserId != userId || !session.IsActive(now))
                {
                    context.Fail("Session is no longer active.");
                    return;
                }

                var isActive = await database.Users.AsNoTracking()
                    .AnyAsync(x => x.Id == userId && x.IsActive, context.HttpContext.RequestAborted);
                if (!isActive)
                    context.Fail("Office user is no longer active.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    foreach (var permission in PermissionCodes.All)
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    application = "NOTAIRE-DESK",
    service = "Office LAN API",
    status = "available",
    dataResidency = "on-premises"
})).AllowAnonymous();

app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program
{
}
