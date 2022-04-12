using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiffPatch.Data;
using SuperPatch.Core.Storages;

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

    public virtual async Task EnsureLoadPatchesOrderAsync()
    {
      if (PatchsSet != null) return;

      if (await Storage.EnsureLoadPatchesOrderAsync() == true)
        return;

      var patches_list = await Storage.GetPatchesListAsync();
      // patches_list in some storage can be null 
      if (patches_list == null) return;

      var order = patches_list.Split("\n");

      PatchsSet = new List<PatchFile>();
      foreach (var patchFileName in order)
      {
        if (string.IsNullOrWhiteSpace(patchFileName)) continue;

        // TODO: fix me
        if (patchFileName.ToUpper().EndsWith("AUTOMATED-DOMAIN-SUBSTITUTION.PATCH"))
          continue;

        Console.WriteLine($"Found {patchFileName}");

        var patchFile = new PatchFile()
        {
          FileName = patchFileName,
          Diff = null,
          LoadDelegate = async(patchFile) =>
          {
            Console.WriteLine($"Loading {patchFileName}");
            var contents = Encoding.UTF8.GetString((await Storage.GetPatchAsync(patchFileName)))
                              .Split('\n')
                              .Where(x => !IsLineToRemove(x))
                              .ToList();
            return DiffPatch.DiffParserHelper.Parse(String.Join('\n', contents));
          }
        };
        PatchsSet.Add(patchFile);
      }
    }

    public static bool IsLineToRemove( string line)
    {
      return line == "-- " ||
             line == "2.17.1" ||
             line == "2.20.1";
    }

    public List<IFileDiff> GetFilesName(List<PatchFile> patchsSet)
    {
      var allFiles = patchsSet
                        .Where(x => x != null)
                        .SelectMany(x => x.Diff)
                        .Where(x => x != null)
                        .Cast<IFileDiff>()
                        .ToList();

      // remove new files from download
      allFiles = allFiles
                      .Where(x => x != null && x.From != "/dev/null")
                      .GroupBy(x => x.From)
                      .Select(x => x.FirstOrDefault())
                      .ToList();
      return allFiles;
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

    public virtual async Task ProcessPatchedFileAsync(FilePatchedContents file)
    {
      await Storage.ProcessPatchedFileAsync(file);
    }
  }
}
