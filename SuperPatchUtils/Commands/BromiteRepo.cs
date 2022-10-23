using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Status;
using SuperPatch.Core.Storages;
using SuperPatch.Core.Storages.Bromite;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public static class BromiteRepo
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return new[] {
        new Command("bromite")
        {
          new Argument<string>("commitshaortag", "Bromite Commit hash"),
          new Argument<string>("outputdir", "The output directory"),
          new Option("--verbose", "Verbose mode"),
          new Option("--createpatched", "Verbose mode"),
          new Argument<string>("patchdir", "The patched output directory"),
        }.WithHandler(typeof(BromiteRepo), nameof(Commands.BromiteRepo.DownloadRepoAsync))
      };
    }

    private static async Task<int> DownloadRepoAsync(
            string commitshaortag, string outputdir, string patchdir,
            bool verbose, bool createpatched,
            IConsole console, CancellationToken cancellationToken)
    {
      if (createpatched && !System.IO.Directory.Exists(patchdir))
      {
        console.Error.Write($"Error: directory {createpatched} doesn't exists");
        return 1;
      }

      var wrkBromite = 
        await DownloadAsync(commitshaortag, outputdir, verbose, console, cancellationToken);

      if (createpatched)
      {
        await Commons.PatchFiles(wrkBromite, patchdir, console,
          new SuperPatch.Core.Status.StatusDelegate());
      }
      return 0;
    }

    public static async Task<RepoData> DownloadAsync(
                string commitshaortag, string outputdir, bool verbose,
                IConsole console, CancellationToken cancellationToken)
    {
      var wrk = new Workspace()
      {
        CommitShaOrTag = commitshaortag
      };
      var httpClient = new System.Net.Http.HttpClient();
      httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; SuperPatch/1.0)");
      wrk.Storage = new BromiteRemoteStorage(wrk, httpClient);
     
      await wrk.EnsureLoadPatchesOrderAsync();

      await wrk.EnsureLoadAllPatches(new SuperPatch.Core.Status.NoopStatusDelegate());

      var allFiles = wrk.GetFilesName(wrk.PatchsSet);
      var failed = new List<IFileDiff>();

      return await Commons.DoFetchAndStore(outputdir, wrk, allFiles, failed);
    }
  }
}
