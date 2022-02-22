using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DiffPatch.Data;
using SuperPatch.Core.Status;

namespace SuperPatch.Core
{
  public class PatchView
  {
    public Workspace Workspace { get; set; }
    public IEnumerable<PatchFile> CurrentPatchs { get; set; }
    public List<FilePatchedContents> Files { get; set; }

    public void UpdateFiles()
    {
      Files = new List<FilePatchedContents>();
      foreach (var currentPatch in CurrentPatchs)
      {
        if (currentPatch.Status == PatchStatus.Loaded)
        {
          foreach (var file in currentPatch.Diff)
          {
            var content = new FilePatchedContents()
            {
              FileName = file.Type == DiffPatch.Data.FileChangeType.Add ? file.To : file.From,
              ChangeType = file.Type,
              Status = FileContentsStatus.NotLoaded,
              Diff = file
            };
            Files.Add(content);
          }
        }
      }
    }
  }

  public class FilePatchedContents : FileContents
  {
    public List<PatchFile> Applied { get; set; }
    public FileChangeType ChangeType { get; set; }
    public FileContents OriginalContents { get; set; }
    public FileDiff Diff { get; internal set; }
  }

  public class PatchViewBuilder
  {
    public static async Task<PatchView> CreateAsync(Workspace wrk, IEnumerable<PatchFile> patchFiles, StatusDelegate status)
    {
      await wrk.EnsureLoadPatches(patchFiles, status);

      var view = new PatchView()
      {
        Workspace = wrk,
        CurrentPatchs = patchFiles
      };
      view.UpdateFiles();
      return view;
    }

    public static async Task<FilePatchedContents> BuildAsync(PatchView view, string fileName, StatusDelegate status)
    {
      var w = await status?.BeginWork("Preparing view");
      try
      {
        var file = view.Files.First(x => x.FileName == fileName);
        if (file.Status == FileContentsStatus.Loaded) return file;

        file.Status = FileContentsStatus.Loading;

        var content = await FileContentBuilder.LoadAsync(view.Workspace, file.Diff, status);
        file.OriginalContents = content;
        file.Contents = content.Contents;

        var patchsToLoad = new List<PatchFile>();
        foreach (var patch in view.Workspace.PatchsSet)
        {
          patchsToLoad.Add(patch);
          if (view.CurrentPatchs.Contains(patch)) break;
        }
        await view.Workspace.EnsureLoadPatches(patchsToLoad, status);

        foreach (var patch in view.Workspace.PatchsSet)
        {
          await patch.EnsureLoad(status);
          foreach (var diff in patch.Diff)
          {
            if (file.FileName == diff.To)
            {
              string patched;
              patched = await view.Workspace.Storage.ApplyPatchAsync(file, diff);

              if (view.CurrentPatchs.Contains(patch))
              {
                file.Contents = patched;
                file.Status = FileContentsStatus.Loaded;
                await status.InvokeAsync($"{file.FileName} ready.");
              }
              else
              {
                file.OriginalContents.Contents = patched;
                file.Contents = patched;

                if (file.Applied == null) file.Applied = new List<PatchFile>();
                file.Applied.Insert(0, patch);
              }
            }
          }

          if (view.CurrentPatchs.Contains(patch)) break;
        }

        await view.Workspace.ProcessPatchedFileAsync(file);

        return file;
      }
      finally
      {
        w?.EndWork();
      }
    }
  }
}