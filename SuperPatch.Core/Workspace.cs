using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SuperPatch.Core
{
  public class Workspace
  {
    [JsonIgnore]
    public Storage Storage { get; set; }
    public string CommitShaOrTag { get; set; }
    public string CommitMessage { get; set; }

    public List<PatchFile> PatchsSet { get; set; }
    public List<Workspace> RelatedCommits { get; set; }

    public async Task EnsureLoadPatchesOrderAsync()
    {
      if (PatchsSet != null) return;

      var patches_list = await Storage.GetPatchesListAsync();
      var order = patches_list.Split("\n");

      PatchsSet = new List<PatchFile>();
      foreach (var patchFileName in order)
      {
        if (string.IsNullOrWhiteSpace(patchFileName)) continue;

        // TODO: fix me
        if (patchFileName.ToUpper().EndsWith("AUTOMATED-DOMAIN-SUBSTITUTION.PATCH"))
          continue;

        Console.WriteLine($"Loading {patchFileName}");

        var patchFile = new PatchFile()
        {
          FileName = patchFileName,
          Diff = null,
          LoadDelegate = async() =>
          {
            var contents = (await Storage.GetPatchAsync(patchFileName))
                              .Split('\n')
                              .Where(x => x != "-- ") // TODO: fix me
                              .Where(x => x != "2.17.1")
                              .ToList();
            return DiffPatch.DiffParserHelper.Parse(String.Join('\n', contents));
          }
        };
        PatchsSet.Add(patchFile);
      }
    }

    public async Task EnsureLoadAllPatches(Status.StatusDelegate status)
    {
      await EnsureLoadPatches(PatchsSet, status);
    }

    public async Task EnsureLoadPatches(IEnumerable<PatchFile> patchsToLoad, Status.StatusDelegate status)
    {
      if (patchsToLoad.All(x => x.Status == PatchStatus.Loaded)) return;

      var w = await status?.BeginWork($"Loading patches for @{this.CommitShaOrTag}");
      try
      {
        await Task.WhenAll(patchsToLoad.Select((x) => x.EnsureLoad(status)));
      }
      finally
      {
        w?.EndWork();
      }
    }

  }
}
