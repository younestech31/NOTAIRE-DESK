var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();

// Bind to the office LAN only at deployment; do not expose this API publicly.
app.MapGet("/", () => Results.Ok(new
{
    application = "NOTAIRE-DESK",
    service = "Office LAN API",
    status = "development",
    dataResidency = "on-premises"
}));
app.MapHealthChecks("/health");
app.Run();

public partial class Program
{
}
