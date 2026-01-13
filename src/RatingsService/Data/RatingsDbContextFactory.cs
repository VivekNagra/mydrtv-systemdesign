using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RatingsService.Data;

public class RatingsDbContextFactory : IDesignTimeDbContextFactory<RatingsDbContext>
{
    public RatingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RatingsDbContext>();

        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=ratingsdb;Username=postgres;Password=postgres";

        options.UseNpgsql(conn);
        return new RatingsDbContext(options.Options);
    }
}
