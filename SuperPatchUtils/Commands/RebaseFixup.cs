using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  internal class RebaseFixup
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return
      [
        new Command("rebasefixup")
        {
          new Argument<string>("sourcefile", "git-rebase-todo file"),
          new Argument<string>("outputfile", "output file"),
        }.WithHandler(typeof(RebaseFixup), nameof(ParseFile)),
      ];
    }

    private sealed class Data
    {
      public string Method { get; set; }
      public string Hash { get; set; }
      public string Title { get; set; }
      public bool ContainsFixup { get; internal set; }
      public string TitleWithoutFixup { get; internal set; }
      public bool Done { get; internal set; }

      public string GetLine()
        => $"{Method} {Hash} {Title}";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    private static async Task<int> ParseFile(
        string sourcefile, string outputfile,
        IConsole console, CancellationToken cancellationToken)
    {
      List<Data> dataList = [];

      var contents = await File.ReadAllTextAsync(sourcefile, cancellationToken);
      var lines = contents.Replace("\r", string.Empty).Split('\n');

      // load data
      foreach (var line in lines)
      {
        var fields = line.Split(' ');
        if (fields.Length > 3)
        {
          var data = new Data
          {
            Method = fields[0],
            Hash = fields[1],
            Title = string.Join(' ', fields.Skip(2).ToList()),
          };
          data.ContainsFixup = data.Title?.Contains("fixup") ?? false;

          if (data.ContainsFixup)
          {
            data.Method = "fixup";
            data.TitleWithoutFixup = data.Title.Replace("(fixup)", string.Empty).Trim();
          }

          dataList.Add(data);
        }
      }

      var listFixup = dataList.Where(x => x.ContainsFixup).ToList();
      var newList = new List<Data>();

      foreach (var item in dataList)
      {
        if (!item.ContainsFixup)
        {
          newList.Add(item);

          var fixupItems = listFixup.Where(x =>
            item.Title.StartsWith(x.TitleWithoutFixup)).ToList();

          fixupItems.ForEach(x =>
          {
            x.Done = true;
            newList.Add(x);
          });
        }
      }

      var output = string.Join("\n", newList.Select(x => x.GetLine()));

      var notDone = listFixup.Where(x => !x.Done).ToList();
      output += "\nCHECK:";
      output += string.Join("\n", notDone.Select(x => x.GetLine()));

      await File.WriteAllTextAsync(outputfile, output, cancellationToken);

      return 0;
    }
  }
}
