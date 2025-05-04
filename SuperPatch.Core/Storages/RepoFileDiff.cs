using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public class RepoFileDiff : FileDiff
  {
    internal GitHubApi.TreeItem treeItem;
    private readonly RepoFileDiff Parent;

    public RepoFileDiff(IFileDiff parent, GitHubApi.TreeItem treeItem)
    {
      this.treeItem = treeItem;
      Parent = parent as RepoFileDiff;

      var path = string.Empty;

      var kParent = Parent;
      while (kParent != null)
      {
        if (!string.IsNullOrWhiteSpace(path))
          path = "/" + path;
        path = kParent.treeItem.path + path;
        kParent = kParent.Parent;
      }

      if (!string.IsNullOrWhiteSpace(path))
        path += "/";
      path += treeItem?.path;

      From = To = path;
      Type = FileChangeType.Modified;
    }

    public bool IsLoaded { get; internal set; }
  }
}
