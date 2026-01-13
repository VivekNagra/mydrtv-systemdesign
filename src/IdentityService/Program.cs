using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using IdentityService.Auth;
using IdentityService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    // Register a Bearer/JWT security scheme for the generated OpenAPI document
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    // Require the Bearer scheme for operations
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null)
            {
                Reference = new OpenApiReferenceWithDescription
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<IdentityDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Db"),
        npgsql => npgsql.EnableRetryOnFailure(5))
);

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role"
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

app.MapPost("/auth/register", async (RegisterRequest req, IdentityDbContext db) =>
{
    var email = req.Email.Trim().ToLowerInvariant();

    var exists = await db.Users.AnyAsync(u => u.Email == email);
    if (exists) return Results.Conflict(new { message = "Email already registered." });

    var user = new User
    {
        Email = email,
        DisplayName = req.DisplayName.Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new { user.Id, user.Email, user.DisplayName });
});

app.MapPost("/auth/login", async (LoginRequest req, IdentityDbContext db) =>
{
    var email = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (user is null) return Results.Unauthorized();

    var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
    if (!ok) return Results.Unauthorized();

    var token = IssueToken(user, jwt);
    return Results.Ok(new { token, userId = user.Id, user.DisplayName });
});

app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var userId =
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    var email =
        user.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? user.FindFirstValue("email")
        ?? user.FindFirstValue(ClaimTypes.Email);

    var displayName =
        user.FindFirstValue("displayName")
        ?? user.FindFirstValue("name");

    return Results.Ok(new { userId, email, displayName });
})
.RequireAuthorization();

app.Run();

static string IssueToken(User user, JwtOptions jwt)
{
    var creds = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("displayName", user.DisplayName)
    };

    var token = new JwtSecurityToken(
        issuer: jwt.Issuer,
        audience: jwt.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

static async Task ApplyMigrationsWithRetry(WebApplication app)
{
    const int maxAttempts = 15;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Identity DB migrations applied");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex,
                "DB migration attempt {Attempt}/{Max} failed. Retrying in {Delay}...",
                attempt, maxAttempts, delay);

            await Task.Delay(delay);
        }
    }

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
    }
}

record RegisterRequest(string Email, string DisplayName, string Password);
record LoginRequest(string Email, string Password);
