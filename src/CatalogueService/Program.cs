using CatalogueService;
using CatalogueService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MyDrTv.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CatalogueDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h => { });
    });
});

var app = builder.Build();

// Apply migrations with a small retry, then republish existing catalog to rebuild search indexes
await ApplyMigrationsWithRetry(app);
await RepublishCatalogueToBus(app);

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

// Seed a few programmes if empty (demo-friendly)
app.MapPost("/admin/seed", async (CatalogueDbContext db) =>
{
    if (await db.Programmes.AnyAsync()) return Results.Ok(new { seeded = false });

    var seeded = new[]
    {
        new Programme { Title = "Matador", Year = 1978, Genre = "Drama", Synopsis = "Classic Danish TV series." },
        new Programme { Title = "Olsen-banden", Year = 1968, Genre = "Comedy", Synopsis = "Iconic Danish film series." },
        new Programme { Title = "Borgen", Year = 2010, Genre = "Drama", Synopsis = "Political drama." }
    };

    db.Programmes.AddRange(seeded);
    await db.SaveChangesAsync();

    // Publish to rebuild downstream read models (e.g., Search)
    logger.LogInformation("Seeding catalogue and publishing ProgrammeUpserted events");
    using var scope = app.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
    foreach (var p in seeded)
    {
        await bus.Publish(new ProgrammeUpserted(p.Id, p.Title, p.Year, p.Genre, p.Synopsis));
    }

    return Results.Ok(new { seeded = true });
});

app.MapGet("/programmes", async (CatalogueDbContext db) =>
    await db.Programmes.AsNoTracking().OrderBy(p => p.Title).ToListAsync());

app.MapPost("/programmes", async (CreateProgrammeRequest req, CatalogueDbContext db, IPublishEndpoint bus) =>
{
    var p = new Programme
    {
        Title = req.Title.Trim(),
        Year = req.Year,
        Genre = req.Genre.Trim(),
        Synopsis = req.Synopsis.Trim()
    };

    db.Programmes.Add(p);
    await db.SaveChangesAsync();

    await bus.Publish(new ProgrammeUpserted(p.Id, p.Title, p.Year, p.Genre, p.Synopsis));
    logger.LogInformation("Published ProgrammeUpserted for {ProgrammeId}", p.Id);
    return Results.Created($"/programmes/{p.Id}", p);
});

app.Run();

static async Task ApplyMigrationsWithRetry(WebApplication app)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Catalogue DB migrations applied");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "Catalogue DB migration attempt {Attempt}/{Max} failed. Retrying in {Delay}...", attempt, maxAttempts, delay);
            await Task.Delay(delay);
        }
    }

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        await db.Database.MigrateAsync();
    }
}

static async Task RepublishCatalogueToBus(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
    var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

    var programmes = await db.Programmes.AsNoTracking().ToListAsync();
    foreach (var p in programmes)
    {
        await bus.Publish(new ProgrammeUpserted(p.Id, p.Title, p.Year, p.Genre, p.Synopsis));
    }
}
