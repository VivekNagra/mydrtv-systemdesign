using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CatalogueService.Data;

public class CatalogueDbContextFactory : IDesignTimeDbContextFactory<CatalogueDbContext>
{
    public CatalogueDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogueDbContext>();

        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=cataloguedb;Username=postgres;Password=postgres";

        options.UseNpgsql(conn);
        return new CatalogueDbContext(options.Options);
    }
}
