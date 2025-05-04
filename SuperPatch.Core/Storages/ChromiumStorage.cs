using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public abstract class ChromiumStorage : Storage
  {
    public class SubModulesInfo
    {
      public string Path { get; internal set; }
      public string Url { get; internal set; }
      public string RevisionIndex { get; internal set; }
      public string Revision { get; internal set; }
    }

    protected HttpClient Http { get; private set; }

    protected virtual string FileSourceUrl => @"https://raw.githubusercontent.com/chromium/chromium";

    public string ChromiumCommit { get; protected set; }

    private string _CacheDirectory = null;
    protected string CacheDirectory { get { return _CacheDirectory; } }

    private readonly List<SubModulesInfo> subModulesInfos = [];

    public ChromiumStorage(Workspace wrk, HttpClient http) : base(wrk)
    {
      Http = http;

      subModulesInfos.Add(new SubModulesInfo()
      {
        Path = "v8/",
        Url = "https://raw.githubusercontent.com/v8/v8",
        RevisionIndex = "v8_revision"
      });
      subModulesInfos.Add(new SubModulesInfo()
      {
        Path = "third_party/devtools-frontend/src/",
        Url = "https://raw.githubusercontent.com/ChromeDevTools/devtools-frontend",
        RevisionIndex = "devtools_frontend_revision"
      });
      subModulesInfos.Add(new SubModulesInfo()
      {
        Path = "third_party/skia/",
        Url = "https://raw.githubusercontent.com/google/skia",
        RevisionIndex = "skia_revision"
      });
    }

    protected virtual async Task FetchChromiumCommit()
    {
      if (string.IsNullOrEmpty(ChromiumCommit))
        ChromiumCommit = Workspace.CommitShaOrTag;

      var depsByte = await GetFileAsync("DEPS");
      foreach (var submodule in subModulesInfos)
        submodule.Revision = GetRevision(depsByte, submodule.RevisionIndex);

      await Task.CompletedTask;
    }

    private static string GetRevision(byte[] file, string index)
    {
      string deps = Encoding.UTF8.GetString(file);
      var rev = $"'{index}'";
      foreach (var line in deps.Split("\n"))
      {
        if (line.Contains(rev))
        {
          string value = line.Split(":")[1]
            .Replace("'", "")
            .Replace(",", "")
            .Trim();
          return value;
        }
      }
      throw new NotSupportedException();
    }

    public virtual void SetCacheDirectory(string CacheDirectory)
    {
      _CacheDirectory = CacheDirectory;
    }

    public override async Task<byte[]> GetFileAsync(IFileDiff file)
    {
      if (file.From == "/dev/null") return null;
      return await GetFileAsync(file.From);
    }

    private async Task<byte[]> GetFileAsync(string file)
    {
      if (_CacheDirectory != null)
      {
        var localFile = System.IO.Path.Combine(_CacheDirectory, file);
        if (System.IO.File.Exists(localFile))
          return await System.IO.File.ReadAllBytesAsync(localFile);
      }

      if (string.IsNullOrEmpty(ChromiumCommit)) await FetchChromiumCommit();

      var url = $"{FileSourceUrl}/{ChromiumCommit}/{file}";
      foreach (var submodule in subModulesInfos)
        if (file.StartsWith(submodule.Path))
          url = $"{submodule.Url}/{submodule.Revision}/{file[submodule.Path.Length..]}";

      try
      {
        var content = await Http.GetByteArrayAsync(url);

        if (_CacheDirectory != null)
        {
          var localFile = System.IO.Path.Combine(_CacheDirectory, file);
          var directory = System.IO.Path.GetDirectoryName(localFile);
          if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

          System.IO.File.WriteAllBytes(localFile, content);
        }

        return content;
      }
      catch (Exception ex)
      {
        throw new Exception($"Url {url} error: {ex}", ex);
      }
    }
  }
}
