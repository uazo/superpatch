using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public abstract class Storage
  {
    protected Workspace workspace { get; private set; }

    public Storage(Workspace wrk)
    {
      this.workspace = wrk;
    }

    public abstract Task<byte[]> GetFileAsync(IFileDiff file);
    public abstract Task<byte[]> GetPatchAsync(string filename);
    public abstract Task<string> GetPatchesListAsync();

    public abstract string StorageName { get; }
    public abstract string GitHubApiEndpoint { get; }

    public abstract string LogoUrl { get; }

    internal protected virtual Task<bool> EnsureLoadPatchesOrderAsync() => Task.FromResult(false);

    public abstract Storage Clone(Workspace wrk);

    public virtual async Task<string> ApplyPatchAsync(FilePatchedContents file, FileDiff diff)
    {
      return await Task.FromResult(DiffPatch.PatchHelper.Patch(file.Contents, diff.Chunks, "\n"));
    }

    public virtual async Task ProcessPatchedFileAsync(FilePatchedContents file) => await Task.CompletedTask;
  }
}