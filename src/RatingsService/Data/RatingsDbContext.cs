using Microsoft.EntityFrameworkCore;

namespace RatingsService.Data;

public class RatingsDbContext : DbContext
{
    public RatingsDbContext(DbContextOptions<RatingsDbContext> options) : base(options) { }
    public DbSet<Rating> Ratings => Set<Rating>();
}

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Guid UserId { get; set; }
    public int Stars { get; set; } // 1..5
    public string? Review { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
