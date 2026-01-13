using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyDrTv.Contracts;
using RatingsService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RatingsDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h => { });
    });
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // Keep incoming claim types as-is (e.g., "sub" stays "sub")
        opt.MapInboundClaims = false;

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply EF Core migrations automatically on startup (with retry)
await ApplyMigrationsWithRetry(app);

var logger = app.Logger;
app.Use(async (ctx, next) =>
{
    var reqId = ctx.TraceIdentifier;
    using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = reqId }))
    {
        logger.LogInformation("HTTP {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        await next();
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/ratings", async (
    CreateRatingRequest req,
    ClaimsPrincipal user,
    RatingsDbContext db,
    IPublishEndpoint bus) =>
{
    // With MapInboundClaims=false, "sub" remains "sub"
    var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
              ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    if (sub is null) return Results.Unauthorized();

    if (req.Stars < 1 || req.Stars > 5)
        return Results.BadRequest(new { message = "Stars must be 1..5" });

    var rating = new Rating
    {
        ProgrammeId = req.ProgrammeId,
        UserId = Guid.Parse(sub),
        Stars = req.Stars,
        Review = string.IsNullOrWhiteSpace(req.Review) ? null : req.Review.Trim()
    };

    db.Ratings.Add(rating);
    await db.SaveChangesAsync();

    await bus.Publish(new RatingSubmitted(
        rating.ProgrammeId,
        rating.UserId,
        rating.Stars,
        rating.Review,
        rating.CreatedAt));
    logger.LogInformation("Published RatingSubmitted for programme {ProgrammeId}", rating.ProgrammeId);

    return Results.Created($"/ratings/{rating.Id}", rating);
}).RequireAuthorization();

app.MapGet("/programmes/{programmeId:guid}/ratings/summary", async (Guid programmeId, RatingsDbContext db) =>
{
    var q = db.Ratings.AsNoTracking().Where(r => r.ProgrammeId == programmeId);
    var count = await q.CountAsync();
    var avg = count == 0 ? 0 : await q.AverageAsync(r => (double)r.Stars);

    return Results.Ok(new
    {
        programmeId,
        count,
        average = Math.Round(avg, 2)
    });
});

static async Task ApplyMigrationsWithRetry(WebApplication app)
{
    const int maxAttempts = 15;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RatingsDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Ratings DB migrations applied");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "Ratings DB migration attempt {Attempt}/{Max} failed. Retrying in {Delay}...",
                attempt, maxAttempts, delay);

            await Task.Delay(delay);
        }
    }

    // Last attempt - bubble exceptions if it still fails
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RatingsDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.Run();

record CreateRatingRequest(Guid ProgrammeId, int Stars, string? Review);
