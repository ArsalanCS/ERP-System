using System.Threading.RateLimiting;
using Erp.Api.Correlation;
using Erp.Api.Errors;
using Erp.Api.Middleware;
using Erp.Api.Security;
using Erp.Application;
using Erp.Application.Abstractions;
using Erp.Application.Auth;
using Erp.Infrastructure;
using Erp.Shared.Correlation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ---------------------------------------------------------------

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // BigInt ids are serialized as strings to survive JS number precision (2^53).
    o.JsonSerializerOptions.Converters.Add(new Erp.Api.Serialization.LongAsStringConverter());
    o.JsonSerializerOptions.Converters.Add(new Erp.Api.Serialization.NullableLongAsStringConverter());
});

// Correlation + current-user context (request-scoped, read from HttpContext).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationContext, HttpCorrelationContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Standard error envelope for unhandled exceptions.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Application + Infrastructure.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Auth: JWT bearer (RS256) + authorization.
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddErpAuthentication(builder.Configuration);

// Rate limiting on auth endpoints (CLAUDE.md §4.5): per-IP fixed window.
var authPermitLimit = builder.Configuration.GetValue("RateLimit:Auth:PermitLimit", 10);
var authWindowSeconds = builder.Configuration.GetValue("RateLimit:Auth:WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromSeconds(authWindowSeconds),
                QueueLimit = 0,
            }));
});

// OpenAPI / Swagger — every endpoint documented (CLAUDE.md §2).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP Platform API",
        Version = "v1",
        Description = "Cloud Accounting & ERP platform — Foundation / Identity module.",
    });
});

// CORS for the local SPA dev server (Vite).
const string SpaCorsPolicy = "SpaDev";
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:5174")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

// ---- HTTP pipeline ----------------------------------------------------------

// Correlation ID must run first so everything downstream (incl. the exception
// handler and logs) can see it.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP Platform API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseCors(SpaCorsPolicy);
}

// Apply migrations + seed the global permission catalog at startup (idempotent).
// Only runs when a database connection is configured.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Erp.Infrastructure.Persistence.ErpDbContext>();
    if (db.Database.GetConnectionString() is { Length: > 0 })
    {
        await db.Database.MigrateAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<Erp.Infrastructure.Persistence.Seeding.IIdentitySeeder>();
        await seeder.SeedPermissionCatalogAsync();
        await seeder.SeedEventAssetCatalogAsync();
        await seeder.SeedMailTemplatesCatalogAsync();
        // Bring existing workspaces up to date (grant new permissions, seed default statuses).
        await seeder.SyncExistingWorkspacesAsync();

        // Development convenience: bootstrap a demo workspace + owner so the SPA is
        // immediately usable. Gated to Development and a config flag; never in prod.
        if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Dev:SeedDemoWorkspace", false))
        {
            await seeder.EnsureDemoWorkspaceAsync(
                app.Configuration.GetValue("Dev:DemoSlug", "demo")!,
                app.Configuration.GetValue("Dev:DemoEmail", "owner@demo.test")!,
                app.Configuration.GetValue("Dev:DemoPassword", "Demo1234!")!);
        }
    }
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

// Resolve tenant scope from the authenticated principal.
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Convenience redirect from root to the API docs in development.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.Run();

// Exposed so the integration test project can use WebApplicationFactory<Program>.
public partial class Program;
