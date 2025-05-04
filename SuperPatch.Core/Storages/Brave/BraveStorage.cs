using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DiffPatch.Data;
using SuperPatch.Core.GitHubApi;

namespace SuperPatch.Core.Storages.Brave
{
  public partial class BraveStorage(Workspace wrk, HttpClient http, ApiService github) : ChromiumStorage(wrk, http)
  {
    public override string StorageName => "Brave";

    public override string GitHubApiEndpoint => @"https://api.github.com/repos/brave/brave-core";

    public override string LogoUrl => "https://avatars.githubusercontent.com/u/39539223";

    protected virtual string PatchSourceUrl => @"https://raw.githubusercontent.com/brave/brave-core";

    public override Storage Clone(Workspace wrk)
    {
      throw new NotImplementedException();
    }

    private string _CacheDirectory;
    public override void SetCacheDirectory(string CacheDirectory)
    {
      _CacheDirectory = System.IO.Path.Combine(CacheDirectory, "brave");
      base.SetCacheDirectory(CacheDirectory);
    }

    protected override async Task FetchChromiumCommit()
    {
      // https://raw.githubusercontent.com/brave/brave-core/master/patches/chrome-VERSION.patch
      var source =
        (await Http.GetStringAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/patches/chrome-VERSION.patch")).Split("\n");

      bool startExtract = false;
      int major = -1, minor = -1, build = -1, patch = -1;
      foreach (var line in source)
      {
        if (line.StartsWith("@@"))
        {
          startExtract = true;
          continue;
        }
        if (!startExtract) continue;

        if (major == -1 && line.Contains("MAJOR=")) major = int.Parse(line[7..]);
        if (minor == -1 && line.Contains("MINOR=")) minor = int.Parse(line[7..]);
        if (build == -1 && line.Contains("BUILD=")) build = int.Parse(line[7..]);
        if (patch == -1 && line.Contains("PATCH=")) patch = int.Parse(line[7..]);
      }

      ChromiumCommit = $"{major}.{minor}.{build}.{patch}"; // es 98.1.37.26
    }

    public async Task QueryChromiumCommit() => await FetchChromiumCommit();

    public override async Task<byte[]> GetFileAsync(IFileDiff file)
    {
      if (file is RepoFileDiff)
        return await GetPatchAsync(file.From);
      else
        return await base.GetFileAsync(file);
    }

    public override async Task<byte[]> GetPatchAsync(string filename)
    {
      if (_CacheDirectory != null)
      {
        var localFile = System.IO.Path.Combine(_CacheDirectory, filename);
        if (System.IO.File.Exists(localFile))
          return await System.IO.File.ReadAllBytesAsync(localFile);
      }

      byte[] contents = null;
      try
      {
        contents = await Http.GetByteArrayAsync($"{PatchSourceUrl}/{Workspace.CommitShaOrTag}/{filename}");
      }
      catch
      {
      }

      if (_CacheDirectory != null)
      {
        var localFile = System.IO.Path.Combine(_CacheDirectory, filename);
        var directory = System.IO.Path.GetDirectoryName(localFile);
        if (!System.IO.Directory.Exists(directory))
          System.IO.Directory.CreateDirectory(directory);

        System.IO.File.WriteAllBytes(localFile, contents);
      }

      return contents;
    }

    public override async Task<string> GetPatchesListAsync()
    {
      var tree = await github.GetTreesAsync(Workspace, Workspace.CommitShaOrTag, false);

      // download patches folder
      var patchsFolder = tree.tree.First(x => x.path == "patches");
      var patchsTree = await github.GetTreesAsync(patchsFolder.url);
      var list = String.Join('\n', patchsTree.tree.Select(x => $"patches/{x.path}"));
      return list;
    }

    public override async Task ProcessPatchedFileAsync(FilePatchedContents file)
    {
      var regEx = MyRegex();

      var sourceBytes = await GetPatchAsync($"chromium_src/{file.FileName}");
      if (sourceBytes == null)
      {
        return;
      }
      var source = System.Text.Encoding.UTF8.GetString(sourceBytes);
      var chrContents = file.Contents;

      file.Contents = string.Empty;

      var defines = new List<Define>();
      var lines = source.Split('\n');
      var linesCount = lines.Length;
      var processDefine = true;
      for (var lineNo = 0; lineNo != linesCount; lineNo++)
      {
        var line = lines[lineNo];

        if (processDefine)
        {
          if (line.Trim().StartsWith($"#define "))
          {
            // found: #define
            lineNo = ParseDefine(lines, lineNo, defines, out var process);
            if (!process)
              file.Contents += line + "\n";
          }
          else if (line.Trim().StartsWith($"#include \"brave/"))
          {
            var groups = regEx.Match(line);
            if (groups.Groups.Count == 2)
            {
              var src = await GetFileAsync(new RepoFileDiff(null, null)
              {
                From = groups.Groups[1].Value["brave/".Length..]
              });

              file.Contents += $"\n// {line}\n\n";
              file.Contents += src;
              file.Contents += $"\n// end: {line}\n";
            }
            else
              file.Contents += line + "\n";
          }
          else if (line.Trim().StartsWith($"#include \"src/{file.FileName}\""))
          {
            // found: #include "file"
            file.Contents += $"\n// {line}\n\n";
            foreach (var def in defines)
              chrContents = chrContents.Replace(def.Name, def.Definition);
            file.Contents += chrContents;
            file.Contents += $"\n// end: {line}\n";

            processDefine = false;
          }
          else
            file.Contents += line + "\n";
        }
        else
          file.Contents += line + "\n";
      }
    }

    private static int ParseDefine(string[] lines, int lineNo, List<Define> defines, out bool process)
    {
      var startLine = lineNo;

      var def = new Define();
      defines.Add(def);

      var line = lines[startLine]["#define ".Length..];
      var fiels = line.Split(" ");
      if (fiels.Length == 1)
      {
        def.Name = line.Trim();
      }
      else
      {
        def.Name = fiels[0].Trim();
        def.Definition = String.Join(' ', fiels.Skip(1)).Trim();
        if (def.Definition.Trim() == "\\") def.Definition = string.Empty;
      }

      if (!line.Trim().EndsWith('\\'))
      {
        process = false;
        return lineNo;
      }

      startLine++;
      for (; ; )
      {
        line = lines[startLine];
        if (string.IsNullOrWhiteSpace(line)) break;
        if (!line.Trim().EndsWith('\\'))
        {
          def.Definition += line.TrimEnd() + "\n";
          break;
        }

        if (def.Definition.Trim() == "\\") def.Definition = string.Empty;
        def.Definition += line[..^1].TrimEnd() + "\n";
        startLine++;
      }

      process = true;
      return startLine;
    }

    private class Define
    {
      internal string Name;
      internal string Definition;
    }

    [System.Text.RegularExpressions.GeneratedRegex("#include \"(.*?)\"")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
  }
}
