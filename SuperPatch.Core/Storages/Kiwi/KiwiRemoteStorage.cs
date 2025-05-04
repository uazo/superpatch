using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages.Kiwi
{
  public class KiwiRemoteStorage(Workspace wrk, HttpClient http, GitHubApi.ApiService githupService) : KiwiStorage(wrk, http)
  {
    public override string StorageName => $"Remote Kiwi repo";

    protected virtual string PatchSourceUrl => $"https://raw.githubusercontent.com/{KiwiRepoName}";

    public override Storage Clone(Workspace wrk) => new KiwiRemoteStorage(wrk, Http, githupService);

    protected override async Task FetchChromiumCommit()
    {
      if (!string.IsNullOrEmpty(ChromiumCommit))
        return;

      ChromiumCommit = "";

      var content = await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/CHROMIUM_VERSION");
      var build = content
                    .Split("\n")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x[(x.IndexOf('=') + 1)..])
                    .ToList();
      ChromiumCommit = string.Join(".", build);
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

    public override Task<string> GetPatchesListAsync() => Task.FromResult<string>(null);

    protected internal override async Task<bool> EnsureLoadPatchesOrderAsync()
    {
      Workspace.PatchsSet =
      [
        new PatchFile()
        {
          FileName = "sources",
          LoadDelegate = PatchFileLoadDelegate
        },
      ];

      return await Task.FromResult(true);
    }

    private async Task<IEnumerable<FileDiff>> PatchFileLoadDelegate(PatchFile patchFile)
    {
      return await LoadSourceSet();
    }

    private async Task<IEnumerable<FileDiff>> LoadSourceSet()
    {
      var tree = await githupService.GetTreesAsync(Workspace, Workspace.CommitShaOrTag, true);
      return ToFileDiff(null, tree);
    }

    private static List<FileDiff> ToFileDiff(IFileDiff parent, GitHubApi.Tree tree)
    {
      var folders = tree.tree
                        .Where(x => x.isTree()).OrderBy(x => x.path)
                        .Select(x => new KiwiFileDiff(parent, x))
                        .Cast<FileDiff>()
                        .ToList();
      var files = tree.tree
                        .Where(x => x.isBlob()).OrderBy(x => x.path)
                        .Select(x => new KiwiFileDiff(parent, x))
                        .Cast<FileDiff>()
                        .ToList();

      return [.. folders.Union(files)];
    }

    public override async Task<byte[]> GetFileAsync(IFileDiff file)
    {
      if (file is KiwiFileDiff kiwiFile)
      {
        if (kiwiFile.treeItem.isTree())
        {
          if (kiwiFile.IsLoaded) return null;

          var patch = Workspace.PatchsSet.First();
          var tree = await githupService.GetTreesAsync(kiwiFile.treeItem.url);
          patch.Diff = [.. patch.Diff.Union(ToFileDiff(file, tree))];
          kiwiFile.IsLoaded = true;
          return null;
        }
      }
      try
      {
        return await base.GetFileAsync(file);
      }
      catch
      {
        file.Type = FileChangeType.Add;
        return null;
      }
    }

    public override async Task<string> ApplyPatchAsync(FilePatchedContents file, FileDiff diff)
    {
      try
      {
        return await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/{file.FileName}");
      }
      catch
      {
        return null;
      }
    }
  }

  public class KiwiFileDiff(IFileDiff parent, GitHubApi.TreeItem treeItem) : RepoFileDiff(parent, treeItem)
  {
  }
}
