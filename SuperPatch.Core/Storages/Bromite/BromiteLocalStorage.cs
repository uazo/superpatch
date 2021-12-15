using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages.Bromite
{
  public class BromiteLocalStorage : BromiteStorage
  {
    private string repoDirectory;

    public BromiteLocalStorage(Workspace workspace, string repoDirectory, HttpClient http)
      : base(workspace, http)
    {
      this.repoDirectory = repoDirectory;
    }

    public override string StorageName => $"Bromite on {repoDirectory}";

    public override Storage Clone(Workspace wrk) => new BromiteLocalStorage(wrk, repoDirectory, http);

    protected override async Task FetchChromiumCommit()
    {
      var file = System.IO.Path.Combine(repoDirectory, "build/RELEASE");
      ChromiumCommit = await System.IO.File.ReadAllTextAsync(file);
    }

    public override async Task<string> GetPatchAsync(string patch)
    {
      var file = System.IO.Path.Combine(
                    System.IO.Path.Combine(repoDirectory, "build/patches"),
                    patch);
      return await System.IO.File.ReadAllTextAsync(file);
    }

    public override async Task<string> GetPatchesListAsync()
    {
      var file = System.IO.Path.Combine(
              System.IO.Path.Combine(repoDirectory, "build"),
              "bromite_patches_list.txt");
      return await System.IO.File.ReadAllTextAsync(file);
    }

    //public override void Download(string file)
    //{
    //  //var address = $"{workspace.SourceUrl}/{workspace.CommitShaOrTag}/{file}";

    //  //var destination = System.IO.Path.Combine(cacheDirectory, file.Replace("/", "\\"));
    //  //var destinationDir = System.IO.Path.GetDirectoryName(destination);
    //  //if (System.IO.Directory.Exists(destinationDir) == false)
    //  //  System.IO.Directory.CreateDirectory(destinationDir);

    //  //Console.WriteLine($"Downloading {address}");
    //  //var client = new WebClient();
    //  //client.DownloadFile(address, destination);
    //}

    //public override async Task<string> GetFileAsync(FileDiff file)
    //{
    //  // TODO: fix it
    //  if (file.Type != FileChangeType.Add && file.From != null)
    //    return await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(repoDirectory, file.From));
    //  else
    //  {
    //    if (file.Type == FileChangeType.Modified)
    //      return await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(repoDirectory, file.To));
    //  }
    //  return string.Empty;
    //}
  }
}
