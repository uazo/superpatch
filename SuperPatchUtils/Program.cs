using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DiffPatch.Data;

using SuperPatch.Core;
using SuperPatch.Core.Storages;

namespace SuperPatchUtils
{
  class Program
  {
    static void Main(string[] args)
    {
      MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args)
    {
      string CacheDirectory = @"C:\Progetti\Uazo\DiffPath\SuperPatch\wwwroot\workspaces";
      await PrepareCacheAsync(CacheDirectory, "92.0.4515.176");
      await PrepareCacheAsync(CacheDirectory, "93.0.4577.83");

      //DownloadSources(wrk);

      //var xx = System.Text.Json.JsonSerializer.Serialize(wrk);
      //System.IO.File.WriteAllText(
      //  System.IO.Path.Combine(CacheDirectory, "wrk.json"),
      //  xx);

      //var yy = System.Text.Json.JsonSerializer.Deserialize<Workspace>(xx);

      //foreach (var p in wrk.PatchsSet)
      //{
      //  var PatchView = await PatchViewBuilder.BuildAsync(wrk, p);
      //} 
    }

    private static async Task PrepareCacheAsync(string cacheDirectory, string CommitShaOrTag)
    {
      var wrk = new Workspace()
      {
        CommitShaOrTag = CommitShaOrTag
      };
      wrk.Storage = new BromiteRemoteStorage(wrk, new System.Net.Http.HttpClient());
      await wrk.EnsureLoadPatchesOrderAsync();

      var wrkFileName = CommitShaOrTag.Replace(".", "_") + ".json";
      var json = System.Text.Json.JsonSerializer.Serialize(wrk);
      System.IO.File.WriteAllText(
        System.IO.Path.Combine(cacheDirectory, wrkFileName),
        json);
    }

    //private static async Task DownloadSourcesAsync(Workspace workspace)
    //{
    //  var allFiles = workspace.PatchsSet
    //                       .Where(x => x != null)
    //                       .SelectMany(x => x.Diff)
    //                       .Where(x => x != null)
    //                       .Select(x => x.From)
    //                       .ToList();

    //  // TODO: fix it
    //  allFiles = workspace.PatchsSet
    //                       .Where(x => x != null)
    //                       .SelectMany(x => x.Diff)
    //                       .Where(x => x != null && x.Type == FileChangeType.Modified && x.From == null)
    //                       .Select(x => x.To)
    //                       .Union(allFiles)
    //                       .ToList();

    //  allFiles = allFiles
    //                  .Where(x => x != null && x != "/dev/null")
    //                  .Distinct().ToList();

    //  // TODO: remove new files from download
    //  var failed = new List<string>();
    //  allFiles.AsParallel().ForAll(async (file) =>
    //  {
    //    try
    //    {
    //      string content = await workspace.Storage.GetPatchAsync(file);
    //      //workspace.Storage.Download(file);
    //    }
    //    catch (System.Exception ex)
    //    {
    //      failed.Add(file);
    //    }
    //  });
    //}
  }
}
