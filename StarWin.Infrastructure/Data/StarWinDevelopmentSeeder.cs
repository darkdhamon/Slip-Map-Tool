using Microsoft.EntityFrameworkCore;

namespace StarWin.Infrastructure.Data;

public static class StarWinDevelopmentSeeder
{
    public static async Task SeedAsync(StarWinDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
    }
}
