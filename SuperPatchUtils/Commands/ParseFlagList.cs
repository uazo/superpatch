using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DiffPatch.Data;
using SuperPatch.Core;
using SuperPatch.Core.Storages;
using SuperPatch.Core.Storages.Bromite;

namespace SuperPatchUtils.Commands
{
  public class ParseFlagList
  {
    public class SymbolsModel
    {
      public string Id { get; set; }
      public string Name { get; set; }
      public string Scope { get; set; }
      public string Signature { get; set; }
      public string Documentation { get; set; }
      public string ReturnType { get; set; }
      public string Type { get; set; }
      public string DefPath { get; set; }
      public int DefStartLine { get; set; }
      public int DefStartCol { get; set; }
      public int DefEndLine { get; set; }
      public int DefEndCol { get; set; }
      public string DeclPath { get; set; }
      public int DeclStartLine { get; set; }
      public int DeclStartCol { get; set; }
      public int DeclEndLine { get; set; }
      public int DeclEndCol { get; set; }
      public int Kind { get; set; }
      public int SubKind { get; set; }
      public int Language { get; set; }
      public int Generic { get; set; }
      public int TemplatePartialSpecialization { get; set; }
      public int TemplateSpecialization { get; set; }
      public int UnitTest { get; set; }
      public int IBAnnotated { get; set; }
      public int IBOutletCollection { get; set; }
      public int GKInspectable { get; set; }
      public int Local { get; set; }
      public int ProtocolInterface { get; set; }

      [IgnoreLoader] public string DefaultValue { get; set; }
      [IgnoreLoader] public string BromiteValue { get; set; }
      [IgnoreLoader] public string AboutFlagName { get; internal set; }
      [IgnoreLoader] public string AboutFlagDescription { get; internal set; }
      [IgnoreLoader] public string AboutFlagOs { get; internal set; }

      public string GetFlagOrScopeName()
      {
        if (AboutFlagName == null)
          return $"{Scope}{Name}";
        else
          return AboutFlagName;
      }
    }

    public class IgnoreLoaderAttribute : Attribute { }

    internal static IEnumerable<Command> GetCommands()
    {
      return new[]
      {
        new Command("parse-flags")
        {
          new Argument<string>("commitshaortag", "Bromite Commit hash or tag"),
          new Argument<string>("sourcefile", "Flag list file path"),
          new Argument<string>("outputdirectory", "Output directory"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(ParseFlagList), nameof(ParseFlagList.ParseFile)),
      };
    }

    private static async Task<int> ParseFile(
            string commitshaortag, string sourcefile, string outputdirectory, bool verbose,
            IConsole console, CancellationToken cancellationToken)
    {
      if (!System.IO.File.Exists(sourcefile))
      {
        console.Error.Write($"Error: file {sourcefile} doesn't exists");
        return 1;
      }

      // load csv
      var flagList = new List<SymbolsModel>();
      await LoadModel(sourcefile, flagList);

      // get chromium files
      var chromiumDirectory = Commons.CombineDirectory(outputdirectory, "chromium");
      await GetFromChromium(commitshaortag, console, flagList, chromiumDirectory);

      // parse value
      ParseValueFromCode(flagList, chromiumDirectory, console, (flag, value) => flag.DefaultValue = value);

      // get bromite files
      var bromiteDirectory = Commons.CombineDirectory(outputdirectory, "bromite");
      var bromitePatchedDirectory = Commons.CombineDirectory(outputdirectory, "bromite-patched");
      await GetFromBromite(commitshaortag, console, bromiteDirectory, bromitePatchedDirectory);

      // parse value
      ParseValueFromCode(flagList, bromitePatchedDirectory, console,
        (flag, value) => flag.BromiteValue = value);

      // parse from about_flags.cc
      ParseValueFromAboutFlags(flagList, bromitePatchedDirectory, console);

      // save json
      string json = JsonSerializer.Serialize(flagList.ToArray());
      System.IO.File.WriteAllText(
        Commons.CombineDirectory(outputdirectory, $"flag-list-{commitshaortag}.json"),
        json);

      // save excel
      ExcelExporter.ExportToExcel(
        Commons.CombineDirectory(outputdirectory, $"flag-list-{commitshaortag}.xlsx"),
        commitshaortag,
        flagList);

      // log results
      console.Out.Write("Bromite changed flags:\n");
      foreach (var field in flagList.Where(x => x.BromiteValue != null && x.DefaultValue != null &&
                                                x.BromiteValue != x.DefaultValue))
        console.Out.Write($"   {field.GetFlagOrScopeName()} {field.DefaultValue} --> {field.BromiteValue}\n");

      console.Out.Write("Bromite new flags:\n");
      foreach (var field in flagList.Where(x => x.BromiteValue != null && x.DefaultValue == null))
        console.Out.Write($"   {field.GetFlagOrScopeName()} = {field.BromiteValue}\n");

      console.Out.Write("Flags without default:\n");
      foreach (var field in flagList.Where(x => x.BromiteValue == null && x.DefaultValue == null))
        console.Out.Write($"   {field.GetFlagOrScopeName()} source file {field.DeclPath}\n");

      return 0;
    }

    private static void ParseValueFromAboutFlags(List<SymbolsModel> flagList, string bromitePatchedDirectory, IConsole console)
    {
      var regEx = new System.Text.RegularExpressions.Regex(",\\s*(?![^()]*\\))(?=([^\"]*\"[^\"]*\")*[^\"]*$)");

      var aboutFlags = System.IO.File.ReadAllText(Commons.CombineDirectory(bromitePatchedDirectory, "chrome/browser/about_flags.cc"));
      var lines = aboutFlags.Split("\n");

      var startIndex = searchForLine(lines, "const FeatureEntry kFeatureEntries[] = {");
      if (startIndex == -1) throw new ApplicationException("kFeatureEntries not found");

      startIndex++;
      var linesCount = lines.Count();
      while (startIndex < linesCount)
      {
        var line = lines[startIndex];
        if (line == "};")
          break;

        if (line.Contains("{"))
        {
          // simply find '}'
          var endIndex = searchForLine(lines, "}", startIndex);
          if (endIndex == -1) throw new ApplicationException($"FATAL: could not find terminator from {startIndex}");

          // get values and remove comments (very simple mode!)
          var flagDefinition = string.Join("",
            lines.Skip(startIndex).Take(endIndex - startIndex + 1)
            .Select(x =>
            {
              if (!x.Contains("//"))
                return x;
              else
                return x.Substring(0, x.IndexOf("//"));
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()));

          // remove { }
          flagDefinition = flagDefinition.Replace("{", string.Empty).Replace("}", string.Empty);

          // get "," indexes
          var matches = regEx.Matches(flagDefinition).Select(x => x.Index + 1).ToArray();
          var fields = flagDefinition.SplitByIndex(matches).Select(x => x.Trim()).ToArray();

          if (fields.Length < 5)
          {
            console.Out.Write($"WARNING: line incomplete '{flagDefinition}'\n");
          }
          else
          {
            if (fields[4].StartsWith("SINGLE_VALUE_TYPE(") ||
                fields[4].StartsWith("SINGLE_DISABLE_VALUE_TYPE(") ||
                fields[4].StartsWith("MULTI_VALUE_TYPE(") ||
                fields[4].StartsWith("FEATURE_VALUE_TYPE(") ||
                fields[4].StartsWith("FEATURE_WITH_PARAMS_VALUE_TYPE(") ||
                fields[4].StartsWith("ENABLE_DISABLE_VALUE_TYPE(") ||
                fields[4].StartsWith("ORIGIN_LIST_VALUE_TYPE(") ||
                fields[4].StartsWith("ENABLE_DISABLE_VALUE_TYPE_AND_VALUE(") ||
                fields[4].StartsWith("SINGLE_VALUE_TYPE_AND_VALUE("))
            {
              var flagName = fields[4].Split('(', ')')[1]
                                      .Split(":")
                                      .Reverse().First();
              if (flagName.Contains(",")) flagName = flagName.Split(',').First();
              var flag = flagList.FirstOrDefault(x => x.Name == flagName);
              if (flag != null)
              {
                flag.AboutFlagDescription = fields[2].Replace(",", string.Empty);
                flag.AboutFlagOs = fields[3].Replace(",", string.Empty);

                if (fields[0].Contains("\""))
                {
                  flag.AboutFlagName = fields[0].Replace("\"", string.Empty).Replace(",", string.Empty);
                }
                else
                {
                  flag.AboutFlagName = fields[0];
                }
              }
              else
              {
                console.Out.Write($"Flag {flagName} not found\n");
              }
            }
            else
            {
              console.Out.Write($"ERROR: line incomplete '{flagDefinition}'\n");
            }
          }
          startIndex = endIndex;
        }

        startIndex++;
      }
    }

    private static async Task GetFromBromite(string commitshaortag, IConsole console, string bromiteDirectory, string bromitePatchedDirectory)
    {
      var statusDelegate = new SuperPatch.Core.Status.StatusDelegate();

      var wrkBromite = await BromiteRepo.DownloadAsync(commitshaortag, bromiteDirectory, true, console, new CancellationToken());
      ((ChromiumStorage)wrkBromite.Workspace.Storage).SetCacheDirectory(bromiteDirectory);

      var countFiles = wrkBromite.Files.Count;
      var indexFile = 1;
      foreach (var file in wrkBromite.Files)
      {
        if (file.To == "/dev/null") continue;

        string patchedFileName = Commons.CombineDirectory(bromitePatchedDirectory, file.To);
        if (!System.IO.File.Exists(patchedFileName))
        {
          console.Out.Write($"[{indexFile}/{countFiles}] Patching {patchedFileName}\n");

          var view = await PatchViewBuilder.CreateAsync(wrkBromite.Workspace, wrkBromite.Workspace.PatchsSet, statusDelegate);
          view.CurrentPatchs = new List<PatchFile>();
          var patched = await PatchViewBuilder.BuildAsync(view, file.To, statusDelegate);
          string directory = System.IO.Path.GetDirectoryName(patchedFileName);
          if (System.IO.Directory.Exists(directory) == false)
            System.IO.Directory.CreateDirectory(directory);

          System.IO.File.WriteAllText(patchedFileName, patched.Contents);
        }
        indexFile++;
      }
    }

    private static async Task GetFromChromium(string commitshaortag, IConsole console, List<SymbolsModel> flagList, string chromiumDirectory)
    {
      var allFiles = flagList.Select(x => x.DefPath)
                          .Where(x => !string.IsNullOrEmpty(x))
                          .Distinct()
                          .OrderBy(x => x)
                          .Select(x => new FileDiff()
                          {
                            From = x
                          })
                          .ToList();
      var wrkChromium = new Workspace()
      {
        CommitShaOrTag = commitshaortag
      };
      wrkChromium.Storage = new BromiteRemoteStorage(wrkChromium, new System.Net.Http.HttpClient());

      var failed = new List<FileDiff>();
      await Commons.DoFetchAndStore(chromiumDirectory, wrkChromium, allFiles, failed);

      if (failed.Count() != 0)
      {
        console.Out.Write("Some files are missings, maybe are specific Bromite files:\n");
        foreach (var f in failed)
          console.Out.Write($"   {f.From}\n");
      }
    }

    private static void ParseValueFromCode(List<SymbolsModel> flagList,
                                   string outputdir,
                                   IConsole console,
                                   Action<SymbolsModel, string /*value*/> assign)
    {
      Action<SymbolsModel, string /*value*/> logAndAssign = (model, value) =>
      {
        console.Out.Write($"{model.Name} = {value}\n");
        assign(model, value);
      };

      foreach (var flag in flagList)
      {
        var sourceFile = Commons.CombineDirectory(outputdir, flag.DefPath);
        if (!System.IO.File.Exists(sourceFile)) continue;

        var content = System.IO.File.ReadAllText(sourceFile);
        var lines = content.Split("\n");

        var searchFor = $"base::Feature {flag.Name}";
        var declLineIndex = searchForLine(lines, searchFor);
        if(declLineIndex != -1 )
        {
          for (; ; )
          {
            var declLine = lines[declLineIndex];
            if (declLine.Contains("base::FEATURE_ENABLED_BY_DEFAULT"))
            {
              logAndAssign(flag, "FEATURE_ENABLED_BY_DEFAULT");
              break;
            }
            else if (declLine.Contains("base::FEATURE_DISABLED_BY_DEFAULT"))
            {
              logAndAssign(flag, "FEATURE_DISABLED_BY_DEFAULT");
              break;
            }
            else if (declLine.Contains("}"))
            {
              break;
            }
            declLineIndex++;
          }
        }
      }
    }

    private static int searchForLine(string[] lines, string substring, int startIndex = 0)
    {
      var linesCount = lines.Count();
      for(var index = startIndex; index < linesCount; index++)
      {
        var line = lines[index];
        if (line.Contains(substring)) return index;
      }
      return -1;
    }

    private static async Task LoadModel(string sourcefile, List<SymbolsModel> flagList)
    {
      var wholeContent = await System.IO.File.ReadAllTextAsync(sourcefile);
      var lines = wholeContent.Split('\n');

      var loader = new ModelLoader<SymbolsModel>();
      var currentFieldNo = 0;
      var currentField = new SymbolsModel();

      // load model from file
      foreach (var line in lines)
      {
        var fields = line.Split("=%=");
        currentFieldNo = loader.Load(currentField, fields, currentFieldNo);

        if (currentFieldNo == 29)
        {
          flagList.Add(currentField);
          currentField = new SymbolsModel();
          currentFieldNo = 0;
        }
        else
        {
          currentFieldNo--;
        }
      }
    }
    private class ModelLoader<T>
    {
      private readonly PropertyInfo[] properties;

      public ModelLoader()
      {
        properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(x => x.GetCustomAttribute<IgnoreLoaderAttribute>() == null)
                     .ToArray();
      }

      internal int Load(T model, string[] fields, int currentIndex)
      {
        foreach (var field in fields)
        {
          if (properties[currentIndex].PropertyType == typeof(string))
          {
            var currentValue = (string)properties[currentIndex].GetValue(model);
            if (string.IsNullOrEmpty(currentValue) == false)
              currentValue += "\n";
            properties[currentIndex].SetValue(model, currentValue + field);
          }
          else if (properties[currentIndex].PropertyType == typeof(int))
          {
            if (int.TryParse(field, out var result))
              properties[currentIndex].SetValue(model, result);
            else if( string.IsNullOrEmpty(field))
              properties[currentIndex].SetValue(model, 0);
            else
              throw new ApplicationException($"Value '{field}' is not an integer");
          }
          else
          {
            throw new ApplicationException($"Unsupported type {properties[currentIndex].PropertyType.Name}");
          }
          currentIndex++;
        }
        return currentIndex;
      }
    }
  }
}
