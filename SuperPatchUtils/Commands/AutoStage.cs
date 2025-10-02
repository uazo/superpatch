using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  public class AutoStage
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return
      [
        new Command("autostage")
        {
          new Argument<string>("sourcefolder", "folder of patch file"),
        }.WithHandler(typeof(AutoStage), nameof(StartAutoStage)),
      ];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    private static async Task<int> StartAutoStage(
      string sourcefolder,
      IConsole console, CancellationToken cancellationToken)
    {
      var files = Directory.GetFiles(sourcefolder, "*.patch");

      var staged = new List<string>();
      foreach (var file in files)
      {
        Console.WriteLine($"git diff {file}");

        var gitDiff = await RunProcessAsync("git",
          $"diff HEAD~1 -- {file}", sourcefolder, default);

        if (gitDiff.ExitCode != 0)
          throw new NotImplementedException("error in git diff");

        var ok = true;
        var lines = gitDiff.Output.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
          string line = lines[i];
          if (line.StartsWith('-') && !line.StartsWith("---"))
          {
            var nextline = lines[i + 1];
            if (line.StartsWith("-@@") && nextline.StartsWith("+@@"))
              ok &= true;
            else
            {
              // Exclude java import
              ok = false;
              break;
            }
          }
        }

        if (ok)
          staged.Add(file);
      }

      foreach (var file in staged)
      {
        Console.WriteLine($"git add {file}");

        var gitAdd = await RunProcessAsync("git",
          $"add {file}", sourcefolder, default);

        if (gitAdd.ExitCode != 0)
          throw new NotImplementedException("error in git add");
      }

      return await Task.FromResult(0);
    }

    private static async Task<(int ExitCode, string Output, string Errors)> RunProcessAsync(
      string application, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
      using (var process = new Process())
      {
        process.StartInfo = new ProcessStartInfo
        {
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
          FileName = application,
          Arguments = arguments,
          WorkingDirectory = workingDirectory,
        };

        var outputBuilder = new StringBuilder();
        var errorsBuilder = new StringBuilder();

        process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
        process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        var exitCode = process.ExitCode;
        var output = outputBuilder.ToString().Trim();
        var errors = errorsBuilder.ToString().Trim();

        return (exitCode, output, errors);
      }
    }
  }
}
