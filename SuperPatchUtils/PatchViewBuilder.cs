using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
  public class PatchView
  {
    public Workspace Workspace { get; set; }
    public PatchFile CurrentPatch { get; set; }

    public List<FileContents> OriginalFiles { get; set; }
    public List<FileContents> PatchedVersion { get; set; }
  }

  public class PatchViewBuilder
  {
    public static PatchView Build(Workspace wrk, PatchFile patchFile)
    {
      var view = new PatchView()
      {
        Workspace = wrk,
        CurrentPatch = patchFile
      };

      view.OriginalFiles = new List<FileContents>();
      foreach (var file in patchFile.Diff)
      {
        var content = FileContentBuilder.Build(wrk, file);
        view.OriginalFiles.Add(content);
      }

      view.PatchedVersion = new List<FileContents>();

      foreach (var patch in wrk.PatchsSet)
      {
        foreach (var chunks in patch.Diff)
        {
          var file = view.OriginalFiles.FirstOrDefault(x => x.FileName == chunks.From);

          if (file != null)
          {
            var patched = DiffPatch.PatchHelper.Patch(file.Contents, chunks.Chunks, "\n");

            var new_contents = new FileContents()
            {
              FileName = file.FileName,
              Contents = patched
            };

            if (patch == view.CurrentPatch)
              view.PatchedVersion.Add(new_contents);
            else
            {
              file.Contents = patched;
              if (file.Applied == null) file.Applied = new List<PatchFile>();
              file.Applied.Add(patch);
            }
          }
        }

        if (patch == view.CurrentPatch) break;
      }

      return view;
    }
  }
}