using Microsoft.EntityFrameworkCore;

namespace RecommendationService.Data;

public class RecsDbContext : DbContext
{
    public RecsDbContext(DbContextOptions<RecsDbContext> options) : base(options) { }
    public DbSet<UserLikeGenre> UserLikeGenres => Set<UserLikeGenre>();
}

public class UserLikeGenre
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Genre { get; set; } = default!;
    public int Likes { get; set; }
}
