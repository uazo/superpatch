using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SuperPatchUtils.Commands.Flags;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public class ConsolidateFlagList
  {

    internal static IEnumerable<Command> GetCommands()
    {
      return
      [
        new Command("consolidate-flags")
        {
            new Argument<string>("sourcedirectory", "Flag list directory path"),
            new Argument<string>("outputfile", "Output file"),
            new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(ConsolidateFlagList), nameof(ConsolidateFlagList.ConsolidateFlags)),
      ];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    private static async Task<int> ConsolidateFlags(
        string sourcedirectory, string outputfile, bool verbose,
        IConsole console, CancellationToken cancellationToken)
    {
      if (!System.IO.Directory.Exists(sourcedirectory))
      {
        console.Error.Write($"Error: directory {sourcedirectory} doesn't exists");
        return 1;
      }

      var orderedList = System.IO.Directory.EnumerateFiles(sourcedirectory)
        .Select(x => new
        {
          PathFile = x,
          FileName = System.IO.Path.GetFileName(x),
          Version = System.IO.Path.GetFileName(x).Replace("flags-list-", "").Split(".")
        })
        .Where(x => System.IO.Path.GetFileName(x.FileName).StartsWith("flags-list-"))
        .OrderBy(x => int.Parse(x.Version[3]) * 1 +
                      int.Parse(x.Version[2]) * 100 +
                      int.Parse(x.Version[1]) * 10000 +
                      int.Parse(x.Version[0]) * 1000000)
        .Select(x => x.PathFile)
        .ToList();

      var result = new ConsolidateList
      {
        Symbols = [],
        Versions = []
      };

      foreach (var file in orderedList)
      {
        var currentVersion = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
        if (!currentVersion.StartsWith("flags-list-"))
        {
          console.Out.Write($"Ignoring {currentVersion}\n");
          continue;
        }

        // get current version from file name
        currentVersion = currentVersion.Replace("flags-list-", "");
        if (result.Versions.Any(x => x.Build == currentVersion))
          continue;

        var version = new Version()
        {
          Id = result.Versions.Count + 1,
          Build = currentVersion,
        };
        result.Versions.Add(version);

        var content = System.IO.File.ReadAllText(file);
        var flagList = JsonSerializer.Deserialize<List<SymbolsModel>>(content);

        // find new flags
        foreach (var flag in flagList)
        {
          var Status = SymbolStatus.None;
          var presentFlag = result.Symbols.FirstOrDefault(x => x.Name == flag.Name);
          if (presentFlag == null)
          {
            Status = SymbolStatus.Added;
            presentFlag = new Flag()
            {
              Name = flag.Name,
              Commits = [],
              FirstSeenAt = version.Id
            };
            result.Symbols.Add(presentFlag);
          }

          presentFlag.Commits.Add(new SymbolVersion()
          {
            Version = version.Id,
            Symbol = flag,
            Status = Status
          });
        }
      }

      // save json
      string json = JsonSerializer.Serialize(result);
      System.IO.File.WriteAllText(
        outputfile,
        json);

      return await Task.FromResult(0);
    }
  }
}
