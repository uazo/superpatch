using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Reflection;
using System.Threading;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Storages;

namespace SuperPatchUtils.Commands
{
  public static class LocalRepo
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return new[]
      {
        new Command("local")
        {
          new Argument<string>("repodir", "Bromite repo local directory"),
          new Argument<string>("outputdir", "The output directory"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(nameof(Commands.LocalRepo.DownloadAsync)),
      };
    }

    private static async Task<int> DownloadAsync(
            string repodir, string outputdir, bool verbose,
            IConsole console, CancellationToken cancellationToken)
    {
      var wrk = new Workspace()
      {
        CommitShaOrTag = System.IO.File.ReadAllText(System.IO.Path.Combine(repodir, "build/RELEASE"))
      };
      wrk.Storage = new BromiteLocalStorage(wrk, repodir, new System.Net.Http.HttpClient());
      await wrk.EnsureLoadPatchesOrderAsync();

      return 0;
    }

    //private static async Task PrepareCacheAsync(string cacheDirectory, string CommitShaOrTag)
    //{
    //  var wrk = new Workspace()
    //  {
    //    CommitShaOrTag = CommitShaOrTag
    //  };
    //  wrk.Storage = new BromiteRemoteStorage(wrk, new System.Net.Http.HttpClient());
    //  await wrk.EnsureLoadPatchesOrderAsync();

    //  var wrkFileName = CommitShaOrTag.Replace(".", "_") + ".json";
    //  var json = System.Text.Json.JsonSerializer.Serialize(wrk);
    //  System.IO.File.WriteAllText(
    //    System.IO.Path.Combine(cacheDirectory, wrkFileName),
    //    json);
    //}
  }
}
