using Microsoft.EntityFrameworkCore;

namespace SearchService.Data;

public class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options) { }

    public DbSet<ProgrammeSearch> Programmes => Set<ProgrammeSearch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProgrammeSearch>(entity =>
        {
            entity.HasKey(x => x.ProgrammeId);

            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Genre).IsRequired();
            entity.Property(x => x.Synopsis).IsRequired();

            entity.HasIndex(x => x.Title);
            entity.HasIndex(x => x.Year);
            entity.HasIndex(x => x.Genre);
        });
    }
}

public class ProgrammeSearch
{
    public Guid ProgrammeId { get; set; }
    public string Title { get; set; } = default!;
    public int Year { get; set; }
    public string Genre { get; set; } = default!;
    public string Synopsis { get; set; } = default!;
    public int RatingsCount { get; set; }
    public double RatingsAverage { get; set; }
}
