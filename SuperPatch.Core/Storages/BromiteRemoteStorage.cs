using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.Storages
{
  public class BromiteRemoteStorage : RemoteChromiumStorage
  {
    public BromiteRemoteStorage(Workspace wrk, HttpClient http) : base(wrk, http) { }

    public override string StorageName => $"Bromite repo";

    protected override string FileSourceUrl => @"https://raw.githubusercontent.com/chromium/chromium";

    protected override string PatchSourceUrl => @"https://raw.githubusercontent.com/bromite/bromite";

    public override string GitHubApiEndpoint => @"https://api.github.com/repos/bromite/bromite";

    public override string LogoUrl => @"https://www.bromite.org/bromite.png";

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

    public override Storage Clone(Workspace wrk)
    {
      return new BromiteRemoteStorage(wrk, http);
    }
  }
}
