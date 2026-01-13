using Microsoft.EntityFrameworkCore;

namespace CatalogueService.Data;

public class CatalogueDbContext : DbContext
{
    public CatalogueDbContext(DbContextOptions<CatalogueDbContext> options) : base(options) { }
    public DbSet<Programme> Programmes => Set<Programme>();
}

public class Programme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public int Year { get; set; }
    public string Genre { get; set; } = default!;
    public string Synopsis { get; set; } = default!;
}
