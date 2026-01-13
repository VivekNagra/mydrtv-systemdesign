using MassTransit;
using Microsoft.EntityFrameworkCore;
using SearchService.Consumers;
using SearchService.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SearchDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProgrammeUpsertedConsumer>();
    x.AddConsumer<RatingSubmittedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

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

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/search", async (
    string? query,
    int? year,
    string? genre,
    double? minRating,
    SearchDbContext db) =>
{
    var q = db.Programmes.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(query))
    {
        var t = query.Trim().ToLowerInvariant();
        q = q.Where(p =>
            p.Title.ToLower().Contains(t) ||
            p.Synopsis.ToLower().Contains(t));
    }

    if (year is not null) q = q.Where(p => p.Year == year);
    if (!string.IsNullOrWhiteSpace(genre)) q = q.Where(p => p.Genre.ToLower() == genre.Trim().ToLower());
    if (minRating is not null) q = q.Where(p => p.RatingsAverage >= minRating);

    var results = await q
        .OrderByDescending(p => p.RatingsAverage)
        .ThenBy(p => p.Title)
        .Take(50)
        .ToListAsync();

    return Results.Ok(results);
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
            var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Search DB migrations applied");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "Search DB migration attempt {Attempt}/{Max} failed. Retrying in {Delay}...",
                attempt, maxAttempts, delay);

            await Task.Delay(delay);
        }
    }

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.Run();
