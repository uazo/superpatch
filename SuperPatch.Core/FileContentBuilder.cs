using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core
{
  public enum FileContentsStatus
  {
    NotLoaded,
    Loading,
    Loaded
  }

  public class FileContents
  {
    public string FileName { get; set; }
    public string Contents { get; set; }
    public FileContentsStatus Status { get; set; } = FileContentsStatus.NotLoaded;
  }

  public class FileContentBuilder
  {
    internal static async Task<FileContents> LoadAsync(Workspace wrk, FileDiff file, Status.StatusDelegate status)
    {
      status?.InvokeAsync($"Loading {file.From}");
      string fileContent = await wrk.Storage.GetFileAsync(file);
      status?.InvokeAsync($"Loaded {file.From}");

      var contents = new FileContents()
      {
        FileName = file.From,
        Contents = fileContent,
        Status = FileContentsStatus.Loaded
      };
      return contents;
    }
  }
}