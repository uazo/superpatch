using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public class RepoFileDiff : FileDiff
  {
    internal GitHubApi.TreeItem treeItem;
    private RepoFileDiff Parent;

    public RepoFileDiff(IFileDiff parent, GitHubApi.TreeItem treeItem)
    {
      this.treeItem = treeItem;
      this.Parent = parent as RepoFileDiff;

      var path = string.Empty;

      var kParent = this.Parent;
      while (kParent != null)
      {
        if (string.IsNullOrWhiteSpace(path) == false)
          path = "/" + path;
        path = kParent.treeItem.path + path;
        kParent = kParent.Parent;
      }

      if (string.IsNullOrWhiteSpace(path) == false)
        path += "/";
      path += treeItem?.path;

      this.From = this.To = path;
      this.Type = FileChangeType.Modified;
    }

    public bool IsLoaded { get; internal set; }
  }
}
