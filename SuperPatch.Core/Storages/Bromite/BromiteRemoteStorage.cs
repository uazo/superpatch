using System.Net.Http;
using System.Threading.Tasks;

namespace SuperPatch.Core.Storages.Bromite
{
  public class BromiteRemoteStorage(Workspace wrk, HttpClient http) : BromiteStorage(wrk, http)
  {
    public override string StorageName => $"Remote Bromite repo";

    protected virtual string PatchSourceUrl => @"https://raw.githubusercontent.com/bromite/bromite";

    public override Storage Clone(Workspace wrk) => new BromiteRemoteStorage(wrk, Http);

    protected override async Task FetchChromiumCommit()
    {
      ChromiumCommit =
        (await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/build/RELEASE"))
        .Replace("\n", "")
        .Replace("\r", "");
      await base.FetchChromiumCommit();
    }

    public override async Task<byte[]> GetPatchAsync(string filename)
    {
      try
      {
        return await Http.GetByteArrayAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/build/patches/{filename}");
      }
      catch
      {
        // file removed
        return null;
      }
    }

    public override async Task<string> GetPatchesListAsync()
    {
      if (string.IsNullOrEmpty(ChromiumCommit)) await FetchChromiumCommit();
      try
      {
        return await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/build/cromite_patches_list.txt");
      }
      catch
      {
        return await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/build/bromite_patches_list.txt");
      }
    }
  }
}
