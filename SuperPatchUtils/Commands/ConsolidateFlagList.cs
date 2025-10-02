using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SuperPatch.Core.Server.Infrastructure.Context;
using SuperPatchUtils.Commands.Flags;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public static class ConsolidateFlagList
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
            new Option("--db", "Upload to Sql Server"),
        }.WithHandler(typeof(ConsolidateFlagList), nameof(ConsolidateFlagList.ConsolidateFlags)),
      ];
    }

    [SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    [SuppressMessage("Minor Code Smell", "S2737:\"catch\" clauses should do more than rethrow",
      Justification = "<Pending>")]
    private static async Task<int> ConsolidateFlags(
        string sourcedirectory, string outputfile, bool verbose, bool db,
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
        console.Out.Write($"Parsing {file}\n");
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

        var content = await System.IO.File.ReadAllTextAsync(file, cancellationToken);
        var flagList = JsonSerializer.Deserialize<List<SymbolsModel>>(content);

        var version = new Version()
        {
          Id = result.Versions.Count + 1,
          Build = currentVersion,
          FlagList = flagList
        };
        result.Versions.Add(version);

        // find new flags
        console.Out.Write($"  found {flagList.Count} flags\n");
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
      await System.IO.File.WriteAllTextAsync(
        outputfile,
        json, cancellationToken);

      // upload to db
      if (db)
      {
        try
        {
          foreach (var version in result.Versions)
          {
            var context = DataContext.GetContext();
            if (await context.Versions.CountAsync(x => x.Build == version.Build) == 0)
            {
              using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

              console.Out.Write($"Saving {version.Build}\n");

              var count = 0;
              var totals = version.FlagList.Count;
              foreach (var symbol in version.FlagList)
              {
                count++;
                if (count % 100 == 0)
                  console.Out.Write($"  Pending {count}/{totals}\n");

                await context.ExecuteAsync(new DataUtils.StoreProcedure("spAddSymbol")
                  .AddParameter("@Build", version.Build)
                  .AddObjectAsParameters(symbol));
              }

              await transaction.CommitAsync(cancellationToken);
            }
          }
        }
        catch (System.Exception ex)
        {
          throw;
        }
      }

      return await Task.FromResult(0);
    }
  }
}
