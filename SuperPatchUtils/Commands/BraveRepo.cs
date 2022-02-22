using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.GitHubApi;
using SuperPatch.Core.Storages;
using SuperPatch.Core.Storages.Brave;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public class BraveRepo
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return new[] {
        new Command("brave")
        {
          new Argument<string>("commitshaortag", "Brave Commit hash"),
          new Argument<string>("outputdir", "The output directory"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(BraveRepo), nameof(Commands.BraveRepo.DownloadRepoAsync))
      };
    }

    private static async Task<int> DownloadRepoAsync(
        string commitshaortag, string outputdir, bool verbose,
        IConsole console, CancellationToken cancellationToken)
    {
      await DownloadAsync(commitshaortag, outputdir, verbose, console, cancellationToken);
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
      var github = new ApiService(httpClient);
      httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; SuperPatch/1.0)");
      
      var storage = new BraveStorage(wrk, httpClient, github);
      var tempDirectory = Commons.CombineDirectory(outputdir, "temp");
      storage.SetCacheDirectory(tempDirectory);

      wrk.Storage = storage;
      await storage.QueryChromiumCommit();

      console.Out.Write($"Chromium version {storage.ChromiumCommit}\n");

      // download patches folder
      await wrk.EnsureLoadPatchesOrderAsync();
      await wrk.EnsureLoadAllPatches(new SuperPatch.Core.Status.NoopStatusDelegate());

      // download chromium sources
      var allFiles = wrk.GetFilesName(wrk.PatchsSet);
      var failed = new List<IFileDiff>();
      var repo = await Commons.DoFetchAndStore(tempDirectory, wrk, allFiles, failed);

      // patch files
      var statusDelegate = new SuperPatch.Core.Status.StatusDelegate()
      {
        OnChangedAsync = (x) => { console.Out.Write(x); return Task.CompletedTask; }
      };

      //var outDir = Commons.CombineDirectory(outputdir, "out");
      await Commons.PatchFiles(repo, outputdir, console, statusDelegate);

      return repo;
    }
  }
}
