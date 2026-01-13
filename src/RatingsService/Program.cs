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
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/ratings", async (CreateRatingRequest req, ClaimsPrincipal user, RatingsDbContext db, IPublishEndpoint bus) =>
{
    var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (sub is null) return Results.Unauthorized();

    if (req.Stars < 1 || req.Stars > 5) return Results.BadRequest(new { message = "Stars must be 1..5" });

    var rating = new Rating
    {
        ProgrammeId = req.ProgrammeId,
        UserId = Guid.Parse(sub),
        Stars = req.Stars,
        Review = string.IsNullOrWhiteSpace(req.Review) ? null : req.Review.Trim()
    };

    db.Ratings.Add(rating);
    await db.SaveChangesAsync();

    await bus.Publish(new RatingSubmitted(rating.ProgrammeId, rating.UserId, rating.Stars, rating.Review, rating.CreatedAt));
    return Results.Created($"/ratings/{rating.Id}", rating);
}).RequireAuthorization();

app.MapGet("/programmes/{programmeId:guid}/ratings/summary", async (Guid programmeId, RatingsDbContext db) =>
{
    var q = db.Ratings.AsNoTracking().Where(r => r.ProgrammeId == programmeId);
    var count = await q.CountAsync();
    var avg = count == 0 ? 0 : await q.AverageAsync(r => (double)r.Stars);
    return Results.Ok(new { programmeId, count, average = Math.Round(avg, 2) });
});

app.Run();

record CreateRatingRequest(Guid ProgrammeId, int Stars, string? Review);
