using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StarWin.Infrastructure.Data;

public sealed class StarWinDbContextFactory : IDesignTimeDbContextFactory<StarWinDbContext>
{
    public StarWinDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StarWinDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=StarWin;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new StarWinDbContext(optionsBuilder.Options);
    }
}
