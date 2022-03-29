using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Storages;
using SuperPatch.Core.Storages.Bromite;
using SuperPatchUtils.Commands.Flags;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public class ConsolidateFlagList
  {

    internal static IEnumerable<Command> GetCommands()
    {
      return new[]
      {
        new Command("consolidate-flags")
        {
            new Argument<string>("sourcedirectory", "Flag list directory path"),
            new Argument<string>("outputfile", "Output file"),
            new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(ConsolidateFlagList), nameof(ConsolidateFlagList.ConsolidateFlags)),
      };
    }

    private static async Task<int> ConsolidateFlags(
        string sourcedirectory, string outputfile, bool verbose,
        IConsole console, CancellationToken cancellationToken)
    {
      if (!System.IO.Directory.Exists(sourcedirectory))
      {
        console.Error.Write($"Error: directory {sourcedirectory} doesn't exists");
        return 1;
      }

      var result = new ConsolidateList();
      result.Symbols = new List<Flag>();
      result.Versions = new List<Flags.Version>();
      foreach (var file in System.IO.Directory.EnumerateFiles(sourcedirectory).OrderBy(x => x))
      {
        var currentVersion = System.IO.Path.GetFileNameWithoutExtension(file).ToLower();
        if( !currentVersion.StartsWith("flags-list-"))
        {
          console.Out.Write($"Ignoring {currentVersion}\n");
          continue;
        }

        // get current version from file name
        currentVersion = currentVersion.Replace("flags-list-", "");
        if (result.Versions.Count(x => x.Build == currentVersion) != 0)
          continue;

        var version = new Flags.Version()
        {
            Id = result.Versions.Count() + 1,
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
              Commits = new List<SymbolVersion>(),
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
