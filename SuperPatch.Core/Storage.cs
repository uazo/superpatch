using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core
{
  public abstract class Storage
  {
    protected Workspace workspace { get; private set; }

    public Storage(Workspace wrk)
    {
      this.workspace = wrk;
    }

    public abstract Task<string> GetFileAsync(FileDiff file);
    public abstract Task<string> GetPatchAsync(string filename);
    public abstract Task<string> GetPatchesListAsync();

    public abstract string StorageName { get; }
    public abstract string GitHubApiEndpoint { get; }

    public abstract string LogoUrl { get; }

    public abstract Storage Clone(Workspace wrk);
  }
}