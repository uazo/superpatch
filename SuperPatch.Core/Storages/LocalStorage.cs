using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  // TODO: fix me
  public class LocalStorage : Storage
  {
    private string sourceDirectory;
    private string cacheDirectory;

    public LocalStorage(Workspace workspace, string sourceDirectory, string cacheDirectory) : base(workspace)
    {
      this.sourceDirectory = sourceDirectory;
      this.cacheDirectory = cacheDirectory;
    }

    public override string StorageName => $"Local Storage on {sourceDirectory}";

    public override string GitHubApiEndpoint => throw new NotImplementedException();

    public override string LogoUrl => throw new NotImplementedException();

    public override Storage Clone(Workspace wrk)
    {
      throw new NotImplementedException();
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

    public override async Task<string> GetFileAsync(FileDiff file)
    {
      // TODO: fix it
      if (file.Type != FileChangeType.Add && file.From != null)
        return await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(cacheDirectory, file.From));
      else
      {
        if (file.Type == FileChangeType.Modified)
          return await System.IO.File.ReadAllTextAsync(System.IO.Path.Combine(cacheDirectory, file.To));
      }
      return string.Empty;
    }

    public override async Task<string> GetPatchAsync(string patch)
    {
      var file = System.IO.Path.Combine(
                    System.IO.Path.Combine(sourceDirectory, "patches"),
                    patch);
      return await System.IO.File.ReadAllTextAsync(file);
    }

    public override async Task<string> GetPatchesListAsync()
    {
      var file = System.IO.Path.Combine(
              System.IO.Path.Combine(sourceDirectory, "build"),
              "bromite_patches_list.txt");
      return await System.IO.File.ReadAllTextAsync(file);
    }
  }
}
