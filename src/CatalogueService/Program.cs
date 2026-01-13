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

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Seed a few programmes if empty (demo-friendly)
app.MapPost("/admin/seed", async (CatalogueDbContext db) =>
{
    if (await db.Programmes.AnyAsync()) return Results.Ok(new { seeded = false });

    db.Programmes.AddRange(
        new Programme { Title = "Matador", Year = 1978, Genre = "Drama", Synopsis = "Classic Danish TV series." },
        new Programme { Title = "Olsen-banden", Year = 1968, Genre = "Comedy", Synopsis = "Iconic Danish film series." },
        new Programme { Title = "Borgen", Year = 2010, Genre = "Drama", Synopsis = "Political drama." }
    );

    await db.SaveChangesAsync();
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
    return Results.Created($"/programmes/{p.Id}", p);
});

app.Run();

record CreateProgrammeRequest(string Title, int Year, string Genre, string Synopsis);
