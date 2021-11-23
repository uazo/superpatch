using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.Storages
{
  public class BromiteRemoteStorage : BromiteStorage
  {
    public BromiteRemoteStorage(Workspace wrk, HttpClient http) : base(wrk, http) { }

    public override string StorageName => $"Remote Bromite repo";

    protected virtual string PatchSourceUrl => @"https://raw.githubusercontent.com/bromite/bromite";

    public override Storage Clone(Workspace wrk) => new BromiteRemoteStorage(wrk, http);

    protected override async Task FetchChromiumCommit()
    {
      ChromiumCommit =
        (await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/build/RELEASE"))
        .Replace("\n", "")
        .Replace("\r", "");
    }

    public override async Task<string> GetPatchAsync(string filename)
    {
      return await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/build/patches/{filename}");
    }

    public override async Task<string> GetPatchesListAsync()
    {
      return await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/build/bromite_patches_list.txt");
    }
  }
}
