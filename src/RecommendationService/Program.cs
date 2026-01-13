using Microsoft.EntityFrameworkCore;
using RecommendationService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RecsDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

builder.Services.AddHttpClient("search", c =>
{
    c.BaseAddress = new Uri("http://search:8080");
});

builder.Services.AddHttpClient("ratings", c =>
{
    c.BaseAddress = new Uri("http://ratings:8080");
});

var app = builder.Build();

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

// Demo endpoint: recommend based on a preferred genre + minRating
// In presentation, you’ll explain that production would be based on events + user profiles.
app.MapGet("/recommendations", async (string genre, double minRating, IHttpClientFactory http) =>
{
    logger.LogInformation("Fetching recommendations for genre {Genre} minRating {MinRating}", genre, minRating);
    var search = http.CreateClient("search");
    var url = $"/search?genre={Uri.EscapeDataString(genre)}&minRating={minRating}";
    var results = await search.GetStringAsync(url);
    return Results.Text(results, "application/json");
});

app.Run();
