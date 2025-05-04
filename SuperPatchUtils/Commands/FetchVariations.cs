using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public static class FetchVariations
  {
    private const string kDefaultServerUrl = "https://clientservices.googleapis.com/chrome-variations/seed";

    internal static IEnumerable<Command> GetCommands()
    {
      return
      [
        new Command("fetchvariations")
        {
          new Argument<string>("outputdir", "The output directory"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(FetchVariations), nameof(FetchVariationsAsync)),
      ];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    private static async Task<int> FetchVariationsAsync(
        string outputdir, bool verbose,
        IConsole console, CancellationToken cancellationToken)
    {
      byte[] content;

      console.Out.WriteLine($"Fetching {kDefaultServerUrl}");
      using (var client = new HttpClient())
      {
        // see VariationsService::DoFetchFromURL
        content = await client.GetByteArrayAsync(kDefaultServerUrl, cancellationToken);
      }

      // components/variations/proto/variations_seed.proto
      var xx = Variations.VariationsSeed.Parser.ParseFrom(content);

      var yy = xx.Study.SelectMany(x => x?.Experiment)
        .Where(x => x != null)
        .Select(x => new
        {
          Experiment = x
        })
        .Where(x => x.Experiment.FeatureAssociation != null)
        .ToList();
      var zz = yy
        .Select(x => new
        {
          Experiment = x,
          x.Experiment.FeatureAssociation,
          x.Experiment.FeatureAssociation.EnableFeature
        })
        .Where(x => x.FeatureAssociation?.EnableFeature.Count != 0)
        .ToList();

      var aa = zz.Where(x => x.FeatureAssociation.EnableFeature.Contains("DeprecateUnload")).ToList();

      return 0;
    }
  }
}
