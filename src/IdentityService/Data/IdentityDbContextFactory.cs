using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.Data;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();

      
        var conn =
            Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Host=localhost;Port=5432;Database=identitydb;Username=postgres;Password=postgres";

        options.UseNpgsql(conn);
        return new IdentityDbContext(options.Options);
    }
}
