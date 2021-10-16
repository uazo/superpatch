using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SuperPatch.Core.Services;
using SuperPatch.Services;

namespace SuperPatch
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      var builder = WebAssemblyHostBuilder.CreateDefault(args);
      builder.RootComponents.Add<App>("#app");

      builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

      builder.Services.AddAntDesign();
      builder.Services.AddScoped<ResizeListener>();

      builder.Services.AddSingleton<ConfigData>();
      builder.Services.AddSingleton<SimpleCache>();
      builder.Services.AddSingleton<UrlCleaner>();
      
      builder.Services.AddScoped<Core.GitHubApi.ApiService>();

      builder.Services.AddScoped<WorkspaceService>();

      await builder.Build().RunAsync();
    }
  }
}
