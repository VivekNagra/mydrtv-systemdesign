using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RecommendationService.Data;

namespace RecommendationService.Data;

public class RecsDbContextFactory : IDesignTimeDbContextFactory<RecsDbContext>
{
    public RecsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RecsDbContext>();

        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=recsdb;Username=postgres;Password=postgres";

        options.UseNpgsql(conn);
        return new RecsDbContext(options.Options);
    }
}
