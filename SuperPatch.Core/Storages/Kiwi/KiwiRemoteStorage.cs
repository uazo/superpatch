﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages.Kiwi
{
  public class KiwiRemoteStorage : KiwiStorage
  {
    GitHubApi.ApiService githupService;

    public KiwiRemoteStorage(Workspace wrk, HttpClient http, GitHubApi.ApiService githupService)
      : base(wrk, http)
    {
      this.githupService = githupService;
    }

    public override string StorageName => $"Remote Kiwi repo";

    protected virtual string PatchSourceUrl => $"https://raw.githubusercontent.com/{KiwiRepoName}";

    public override Storage Clone(Workspace wrk) => new KiwiRemoteStorage(wrk, http, githupService);

    protected override async Task FetchChromiumCommit()
    {
      if (string.IsNullOrEmpty(ChromiumCommit) == false)
        return;

      ChromiumCommit = "";

      var content = await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/CHROMIUM_VERSION");
      var build = content
                    .Split("\n")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Substring(x.IndexOf("=") + 1))
                    .ToList();
      ChromiumCommit = string.Join(".", build);
    }

    public override async Task<string> GetPatchAsync(string filename)
    {
      try
      {
        return await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/build/patches/{filename}");
      }
      catch
      {
        // file removed
        return string.Empty;
      }
    }

    public override Task<string> GetPatchesListAsync() => Task.FromResult<string>(null);

    protected internal override async Task<bool> EnsureLoadPatchesOrderAsync()
    {
      workspace.PatchsSet = new List<PatchFile>();
      workspace.PatchsSet.Add(new PatchFile()
      {
        FileName = "sources",
        LoadDelegate = PatchFileLoadDelegate
      });

      return await Task.FromResult(true);
    }

    private async Task<IEnumerable<FileDiff>> LoadSourceSet()
    {
      var tree = await githupService.GetTreesAsync(workspace, workspace.CommitShaOrTag, true);
      return ToFileDiff(null, tree);
    }

    private IEnumerable<FileDiff> ToFileDiff(FileDiff parent, GitHubApi.Tree tree)
    {
      var folders = tree.tree
                        .Where(x => x.isTree()).OrderBy(x => x.path)
                        .Select(x => new KiwiFileDiff(parent, x)).ToList();
      var files = tree.tree
                        .Where(x => x.isBlob()).OrderBy(x => x.path)
                        .Select(x => new KiwiFileDiff(parent, x)).ToList();

      return folders.Union(files).ToList();
    }

    private async Task<IEnumerable<FileDiff>> PatchFileLoadDelegate(PatchFile patchFile)
    {
      return await LoadSourceSet();
    }

    public override async Task<string> GetFileAsync(FileDiff file)
    {
      var kiwiFile = file as KiwiFileDiff;
      if (kiwiFile != null)
      {
        if (kiwiFile.treeItem.isTree())
        {
          if (kiwiFile.IsLoaded) return string.Empty;

          var patch = workspace.PatchsSet.First();
          var tree = await githupService.GetTreesAsync(kiwiFile.treeItem.url);
          patch.Diff = patch.Diff.Union(ToFileDiff(file, tree)).ToList();
          kiwiFile.IsLoaded = true;
          return string.Empty;
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
        return await http.GetStringAsync($"{PatchSourceUrl}/{workspace.CommitShaOrTag}/{file.FileName}");
      }
      catch
      {
        return null;
      }
    }
  }

  public class KiwiFileDiff : FileDiff
  {
    internal GitHubApi.TreeItem treeItem;
    private KiwiFileDiff Parent;

    public KiwiFileDiff(FileDiff parent, GitHubApi.TreeItem treeItem)
    {
      this.treeItem = treeItem;
      this.Parent = parent as KiwiFileDiff;

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
      path += treeItem.path;

      this.From = this.To = path;
      this.Type = FileChangeType.Modified;
    }

    public bool IsLoaded { get; internal set; }
  }
}
