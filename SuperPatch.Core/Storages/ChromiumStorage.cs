using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public abstract class ChromiumStorage : Storage
  {
    protected HttpClient http { get; private set; }

    protected virtual string FileSourceUrl => @"https://raw.githubusercontent.com/chromium/chromium";

    public string ChromiumCommit { get; protected set; }

    private string _CacheDirectory = null;
    protected string CacheDirectory { get { return _CacheDirectory; } }

    public ChromiumStorage(Workspace wrk, HttpClient http) : base(wrk)
    {
      this.http = http;
    }

    protected virtual async Task FetchChromiumCommit()
    {
      ChromiumCommit = workspace.CommitShaOrTag;
      await Task.CompletedTask;
    }

    public virtual void SetCacheDirectory( string CacheDirectory )
    {
      _CacheDirectory = CacheDirectory;
    }

    public override async Task<byte[]> GetFileAsync(IFileDiff file)
    {
      if (file.From == "/dev/null") return null;
      if (_CacheDirectory != null)
      {
        var localFile = System.IO.Path.Combine(_CacheDirectory, file.From);
        if( System.IO.File.Exists(localFile))
          return await System.IO.File.ReadAllBytesAsync(localFile);
      }

      if (string.IsNullOrEmpty(ChromiumCommit)) await FetchChromiumCommit();
      var content = await http.GetByteArrayAsync($"{FileSourceUrl}/{ChromiumCommit}/{file.From}");

      if (_CacheDirectory != null)
      {
        var localFile = System.IO.Path.Combine(_CacheDirectory, file.From);
        var directory = System.IO.Path.GetDirectoryName(localFile);
        if (System.IO.Directory.Exists(directory) == false)
          System.IO.Directory.CreateDirectory(directory);

        System.IO.File.WriteAllBytes(localFile, content);
      }

      return content;
    }
  }
}
