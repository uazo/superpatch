using System;
using System.Collections.Generic;
using DiffPatch.Data;

namespace SuperPatch.Core
{
  public class FileContents
  {
    public string FileName { get; set; }
    public string Contents { get; set; }

    public List<PatchFile> Applied { get; set; }
  }

  public class FileContentBuilder
  {
    internal static FileContents Build(Workspace wrk, FileDiff file)
    {
      string fileContent = wrk.Storage.GetFile(file);
      var contents = new FileContents()
      {
        FileName = file.From,
        Contents = fileContent
      };
      return contents;
    }
  }
}