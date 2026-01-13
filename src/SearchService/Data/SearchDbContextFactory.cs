using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SearchService.Data;

public class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>();

        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=searchdb;Username=postgres;Password=postgres";

        options.UseNpgsql(conn);
        return new SearchDbContext(options.Options);
    }
}
