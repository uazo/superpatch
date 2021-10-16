using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core
{
  public enum PatchStatus
  {
    NotLoaded,
    Loading,
    Loaded
  }

  public class PatchFile
  {
    public PatchStatus Status { get; set; } = PatchStatus.NotLoaded;

    public string FileName { get; set; }

    internal Func<Task<IEnumerable<FileDiff>>> LoadDelegate { get; set; }

    public IEnumerable<FileDiff> Diff { get; set; }

    public async Task EnsureLoad(Status.StatusDelegate status)
    {
      if (Diff != null) return;

      Status = PatchStatus.Loading;

      if(status != null)
        await status?.InvokeAsync($"Loading {FileName}");

      Diff = await LoadDelegate();

      Status = PatchStatus.Loaded;
      if (status != null)
        await status?.InvokeAsync($"Loaded {FileName}");
    }
  }
}
