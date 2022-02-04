using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Storages;

namespace SuperPatchUtils.Commands.Utils
{
  public static class Commons
  {
    public static string CombineDirectory(string a, string b) =>
      System.IO.Path.Combine(a,
            b.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public static async Task DoFetchAndStore(string outputdir, Workspace wrk, List<FileDiff> allFiles, List<FileDiff> failed)
    {
      int fileCount = allFiles.Count();
      int currentFile = 0;
      foreach (var file in allFiles)
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

            string content = await wrk.Storage.GetFileAsync(file);

            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (System.IO.Directory.Exists(directory) == false)
              System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllText(filePath, content);
          }
        }
        catch (System.Exception ex)
        {
          Console.WriteLine(ex.Message);
          failed.Add(file);
        }
      }
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
        yield return @string.Substring(previousIndex, index - previousIndex);
        previousIndex = index;
      }

      yield return @string.Substring(previousIndex);
    }
  }
}
