using Microsoft.Extensions.Configuration;

namespace SuperPatch.Core.Server.Infrastructure.Configuration
{
  public sealed class Config
  {
    public string DefaultConnection { get; set; } = default!;
  }

  public static class ConfigHelper
  {
    public static IConfigurationRoot GetIConfigurationBase()
    {
      return new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();
    }

    public static Config GetApplicationConfiguration()
    {
      var systemConfiguration = new Config();

      var iTestConfigurationRoot = GetIConfigurationBase();
      iTestConfigurationRoot
        .GetSection("ConnectionStrings")
        .Bind(systemConfiguration);

      return systemConfiguration;
    }
  }
}
