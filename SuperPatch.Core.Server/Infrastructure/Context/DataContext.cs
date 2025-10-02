using Microsoft.EntityFrameworkCore;
using SuperPatch.Core.Server.Infrastructure.Configuration;

namespace SuperPatch.Core.Server.Infrastructure.Context
{
  public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
  {
    public static DataContext GetContext()
    {
      var config = ConfigHelper.GetApplicationConfiguration();
      return new DataContext(
            new DbContextOptionsBuilder<DataContext>()
                .UseSqlServer(config.DefaultConnection)
                .Options);
    }

    public DbSet<Models.Version> Versions { get; set; }
  }
}
