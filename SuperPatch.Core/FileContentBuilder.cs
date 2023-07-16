using System;
using System.Collections.Generic;
using System.Linq;
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
			string fileContent = null;

			var diffAdd = wrk.PatchsSet
											.SelectMany(x => x.Diff)
											.FirstOrDefault(x => x.Type == FileChangeType.Add &&
																						x.To == file.From);
			if (diffAdd != null)
			{
				fileContent =
					DiffPatch.PatchHelper.Patch(string.Empty, diffAdd.Chunks, "\n");
			}

			if (fileContent == null)
			{
				try
				{
					fileContent = System.Text.Encoding.UTF8.GetString(
						await wrk.Storage.GetFileAsync(file));
				}
				catch (System.Exception ex)
				{
					status?.InvokeAsync($"Error {ex.Message}");
				}
			}
      status?.InvokeAsync($"Loaded {file.From}");

      var contents = new FileContents()
      {
        FileName = file.From,
        Contents = fileContent ?? string.Empty,
        Status = FileContentsStatus.Loaded
      };
      return contents;
    }
  }
}