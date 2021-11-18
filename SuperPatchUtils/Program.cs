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

namespace SuperPatchUtils
{
  static class Program
  {
    static async Task<int> Main(string[] args)
    {
      var cmd = new RootCommand
      {
        new Command("download")
        {
          new Argument<string>("commitshaortag", "Commit hash"),
          new Argument<string>("outputdir", "The output directory"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(nameof(DownloadAsync))
      };

      return await cmd.InvokeAsync(args);
    }

    private static Command WithHandler(this Command command, string methodName)
    {
      var method = typeof(Program).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
      var handler = CommandHandler.Create(method!);
      command.Handler = handler;
      return command;
    }

    static async Task<int> DownloadAsync(
      string commitshaortag, string outputdir, bool Verbose, 
      IConsole console, CancellationToken cancellationToken)
    {
      var wrk = new Workspace()
      {
        CommitShaOrTag = commitshaortag
      };
      wrk.Storage = new BromiteRemoteStorage(wrk, new System.Net.Http.HttpClient());
      await wrk.EnsureLoadPatchesOrderAsync();

      await wrk.EnsureLoadAllPatches(new SuperPatch.Core.Status.NoopStatusDelegate());

      var allFiles = wrk.PatchsSet
                        .Where(x => x != null)
                        .SelectMany(x => x.Diff)
                        .Where(x => x != null)
                        .ToList();

      // remove new files from download
      allFiles = allFiles
                      .Where(x => x != null && x.From != "/dev/null")
                      .GroupBy(x => x.From)
                      .Select( x=> x.FirstOrDefault())
                      .ToList();

      var failed = new List<FileDiff>();
      allFiles.AsParallel().ForAll(async (file) =>
      {
        try
        {
          string content = await wrk.Storage.GetFileAsync(file);

          string filePath = System.IO.Path.Combine(outputdir, 
            file.From.Replace('/', System.IO.Path.DirectorySeparatorChar));

          string directory = System.IO.Path.GetDirectoryName(filePath);
          if (System.IO.Directory.Exists(directory) == false)
            System.IO.Directory.CreateDirectory(directory);

          System.IO.File.WriteAllText(filePath, content);
        }
        catch // (System.Exception ex)
        {
          failed.Add(file);
          throw;
        }
      });

      return 0;
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
  }
}
