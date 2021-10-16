using System;
using System.Net;
using DiffPatch.Data;

namespace ConsoleApp1
{
  public abstract class Storage
  {
    public abstract void Download(string file);
    public abstract string GetFile(FileDiff file);
    public abstract string[] GetPatch(string filename);
    public abstract string GetPatchesList();
  }

  public class DirectoryStorage : Storage
  {
    private string cacheDirectory;
    private Workspace workspace;

    public DirectoryStorage(Workspace workspace, string cacheDirectory)
    {
      this.workspace = workspace;
      this.cacheDirectory = cacheDirectory;
    }

    public override void Download(string file)
    {
      var address = $"{workspace.SourceUrl}/{workspace.CommitShaOrTag}/{file}";

      var destination = System.IO.Path.Combine(cacheDirectory, file.Replace("/", "\\"));
      var destinationDir = System.IO.Path.GetDirectoryName(destination);
      if (System.IO.Directory.Exists(destinationDir) == false)
        System.IO.Directory.CreateDirectory(destinationDir);

      Console.WriteLine($"Downloading {address}");
      var client = new WebClient();
      client.DownloadFile(address, destination);
    }

    public override string GetFile(FileDiff file)
    {
      // TODO: fix it
      if (file.Type != FileChangeType.Add && file.From != null)
        return System.IO.File.ReadAllText(System.IO.Path.Combine(cacheDirectory, file.From));
      else
      {
        if (file.Type == FileChangeType.Modified)
          return System.IO.File.ReadAllText(System.IO.Path.Combine(cacheDirectory, file.To));
      }
      return string.Empty;
    }

    public override string[] GetPatch(string patch)
    {
      var file = System.IO.Path.Combine(
                    System.IO.Path.Combine(workspace.MasterDirectory, "patches"),
                    patch);
      return System.IO.File.ReadAllLines(file);
    }

    public override string GetPatchesList()
    {
      return System.IO.Path.Combine(workspace.MasterDirectory, "bromite_patches_list.txt");
    }
  }

  public class StorageBuilder
  {
    public static Storage Build(Workspace workspace, string cacheDirectory)
    {
      return new DirectoryStorage(workspace, cacheDirectory);
    }
  }
}