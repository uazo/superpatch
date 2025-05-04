using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core;

namespace SuperPatchUtils.Commands.Utils
{
  public static class Commons
  {
    public static string CombineDirectory(string a, string b) =>
      System.IO.Path.Combine(a,
            b.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public static async Task<RepoData> DoFetchAndStore(string outputdir, Workspace wrk, List<IFileDiff> fileToDownload, List<IFileDiff> failed)
    {
      int fileCount = fileToDownload.Count;
      int currentFile = 0;
      foreach (var file in fileToDownload)
      {
        currentFile++;
        try
        {
          string filePath = CombineDirectory(outputdir, file.From);

          if (System.IO.File.Exists(filePath))
          {
            Console.WriteLine($"File already exists {file.From}");
          }
          else
          {
            Console.WriteLine($"[{currentFile}/{fileCount}] Downloading {file.From}");

            var content = await wrk.Storage.GetFileAsync(file);

            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (System.IO.Directory.Exists(directory) == false)
              System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllBytes(filePath, content);
          }
        }
        catch (System.Exception ex)
        {
          Console.WriteLine(ex.Message);
          failed.Add(file);
        }
      }

      return new RepoData()
      {
        Workspace = wrk,
        Files = fileToDownload
      };
    }

    public static Command WithHandler(this Command command, Type ClassType, string methodName)
    {
      var method = ClassType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
      var handler = CommandHandler.Create(method!);
      command.Handler = handler;
      return command;
    }

    public static IEnumerable<string> SplitByIndex(this string @string, params int[] indexes)
    {
      var previousIndex = 0;
      foreach (var index in indexes.OrderBy(i => i))
      {
        yield return @string[previousIndex..index];
        previousIndex = index;
      }

      yield return @string[previousIndex..];
    }

    public static async Task PatchFiles(RepoData repo,
      string outputDirectory,
      IConsole console,
      SuperPatch.Core.Status.StatusDelegate statusDelegate)
    {
      var countFiles = repo.Files.Count;
      var indexFile = 1;
      foreach (var file in repo.Files)
      {
        if (file.To == "/dev/null") continue;

        string patchedFileName = Commons.CombineDirectory(outputDirectory, file.To);
        if (!System.IO.File.Exists(patchedFileName))
        {
          console.Out.Write($"[{indexFile}/{countFiles}] Patching {patchedFileName}\n");

          var view = await PatchViewBuilder.CreateAsync(repo.Workspace, repo.Workspace.PatchsSet, statusDelegate);
          view.CurrentPatchs = [];
          var patched = await PatchViewBuilder.BuildAsync(view, file.To, statusDelegate);
          string directory = System.IO.Path.GetDirectoryName(patchedFileName);
          if (System.IO.Directory.Exists(directory) == false)
            System.IO.Directory.CreateDirectory(directory);

          System.IO.File.WriteAllText(patchedFileName, patched.Contents);
        }
        indexFile++;
      }
    }
  }
}
