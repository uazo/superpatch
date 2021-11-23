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

namespace SuperPatchUtils.Commands
{
  public static class Commons
  {
    public static async Task DoFetchAndStore(string outputdir, Workspace wrk, List<FileDiff> allFiles, List<FileDiff> failed)
    {
      foreach (var file in allFiles)
      {
        try
        {
          Console.WriteLine($"Downloading {file.From}");

          string content = await wrk.Storage.GetFileAsync(file);

          string filePath = System.IO.Path.Combine(outputdir,
            file.From.Replace('/', System.IO.Path.DirectorySeparatorChar));

          string directory = System.IO.Path.GetDirectoryName(filePath);
          if (System.IO.Directory.Exists(directory) == false)
            System.IO.Directory.CreateDirectory(directory);

          System.IO.File.WriteAllText(filePath, content);
        }
        catch (System.Exception ex)
        {
          Console.WriteLine(ex.Message);
          failed.Add(file);
          throw;
        }
      }
    }
    public static Command WithHandler(this Command command, string methodName)
    {
      var method = typeof(Program).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
      var handler = CommandHandler.Create(method!);
      command.Handler = handler;
      return command;
    }
  }
}
