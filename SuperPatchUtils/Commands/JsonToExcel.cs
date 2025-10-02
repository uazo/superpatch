using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SuperPatchUtils.Commands.Utils;

namespace SuperPatchUtils.Commands
{
  internal class JsonToExcel
  {
    internal static IEnumerable<Command> GetCommands()
    {
      return
      [
        new Command("json2excel")
        {
          new Argument<string>("name", "Test name"),
          new Argument<string>("sourcedir", "Input Folder"),
          new Argument<string>("output", "Output excel file"),
          new Argument<string>("pattern", "Input file pattern"),
          new Option("--verbose", "Verbose mode"),
        }.WithHandler(typeof(JsonToExcel), nameof(JsonToExcel.Start)),
      ];
    }

    private sealed class ElementList
    {
      public List<Elements> Elements { get; set; }

      public string Folder { get; set; }
    }

    private sealed class Elements
    {
      public string Key { get; internal set; }
      public List<ElementValue> ValueList { get; internal set; }

      public override string ToString()
      {
        var values = string.Join(',', [.. ValueList.Select(x => x.Value)]);
        return $"{Key}={values}";
      }
    }

    private sealed class ElementValue
    {
      public string Value { get; internal set; }
      public List<string> DevicesName { get; internal set; }

      public override string ToString()
      {
        return $"{Value}/{DevicesName?.Count}";
      }
    }

    private sealed class ElementGroup
    {
      public string Folder { get; set; }
      public string DeviceName { get; internal set; }
      public string Caption { get; internal set; }
    }

    private sealed class ElementOuput
    {
      public List<ElementList> Elements { get; set; }
      public List<string> FoldersName { get; set; }
      public List<string> DevicesName { get; set; }
      public List<string> Keys { get; set; }
    }

    [SuppressMessage("Style",
      "IDE0060:Remove unused parameter",
      Justification = "<Pending>")]
    private static async Task<int> Start(
      string name,
      string sourcedir, string output, string pattern, bool verbose,
      IConsole console, CancellationToken cancellationToken)
    {
      string[] allFolder = Directory.GetDirectories(
        sourcedir,
        "*.*",
        SearchOption.TopDirectoryOnly);

      var result = new ElementOuput()
      {
        Elements = []
      };
      foreach (string folder in allFolder)
      {
        string testName =
          folder.Replace(sourcedir, "")
          .Split(Path.DirectorySeparatorChar)
          .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var elementList = new ElementList()
        {
          Folder = testName,
          Elements = []
        };
        result.Elements.Add(elementList);

        string[] allFiles = Directory.GetFiles(
          folder,
          pattern,
          SearchOption.AllDirectories);

        await ProcessFiles(allFiles, elementList.Elements,
          cancellationToken);
      }

      result.FoldersName = [.. result.Elements
        .Select(x => x.Folder)
        .Distinct()];

      result.DevicesName = [.. result.Elements
        .SelectMany(x => x.Elements)
        .SelectMany(x => x.ValueList)
        .SelectMany(x => x.DevicesName)
        .Distinct()];

      result.Keys = [.. result.Elements
        .SelectMany(x => x.Elements)
        .Select(x => x.Key)
        .Distinct()];

      // Export to excel
      using (var pck = new OfficeOpenXml.ExcelPackage())
      {
        //var wsData = pck.Workbook.Worksheets.Add("data");
        var wsList = pck.Workbook.Worksheets.Add("list");
        AddKeyGroup1(pck);

        List<ElementGroup> elementGroups = [];
        foreach (var device in result.DevicesName)
        {
          foreach (var folder in result.FoldersName)
          {
            elementGroups.Add(new ElementGroup()
            {
              Folder = folder,
              DeviceName = device,
              Caption = $"{device} \"{folder}\""
            });
          }
        }
        elementGroups.Sort((x, y) => string.Compare(x.Caption, y.Caption));

        wsList.Cells[1, 1].Value = "Caption";
        wsList.Cells[1, 2].Value = "DeviceName";
        wsList.Cells[1, 3].Value = "Key";
        wsList.Cells[1, 4].Value = "Value";
        wsList.Cells[1, 5].Value = "Test";

        int dataCol = 2;
        //wsData.Cells[1, 1].Value = "Key";
        //foreach (var item in elementGroups)
        //{
        //	wsData.Cells[1, dataCol++].Value = item.Caption;			
        //}

        var valueCache = new Dictionary<string, List<string>>();

        int listRow = 2;
        dataCol = 2;
        foreach (var item in elementGroups)
        {
          int dataRow = 2;
          foreach (var keyName in result.Keys)
          {
            if (keyName.StartsWith("maths.")) continue;
            //wsData.Cells[dataRow, 1].Value = keyName;

            var elementList = result.Elements.First(x => x.Folder == item.Folder);
            Elements element = FindBy(elementList.Elements, keyName, item.DeviceName);
            if (element != null)
            {
              var sel = element.ValueList
                .Where(x => x.DevicesName.Contains(item.DeviceName))
                .ToList();
              if (sel.Count != 0)
              {
                string value = string.Join(",", sel.Select(x => x.Value));
                if (value.StartsWith("\"data:image"))
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} data";
                }
                else if (keyName.EndsWith(".emojiSet"))
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} emojiiSet";
                }
                else if (keyName.EndsWith(".$hash") ||
                         keyName == "canvasWebgl.pixels" ||
                         keyName == "canvasWebgl.pixels2")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} hash";
                }
                else if (keyName == "offlineAudioContext.binsSample")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} binsSample";
                }
                else if (keyName == "offlineAudioContext.copySample")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} copySample";
                }
                else if (keyName == "media.mimeTypes")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} mimeType";
                }
                else if (keyName == "canvasWebgl.extensions")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} canvas ext";
                }
                else if (keyName == "windowFeatures.keys")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} windowFeatures";
                }
                else if (keyName == "htmlElementVersion.keys")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} htmlElementVersion";
                }
                else if (keyName == "css.computedStyle.keys")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} computedStyle";
                }
                else if (keyName == "css.system.colors")
                {
                  List<string> cache = GetCachedValue(valueCache, keyName, value);
                  value = $"#{cache.IndexOf(value)} color";
                }
                else if (keyName == "clientRects.elementClientRects" ||
                  keyName == "clientRects.elementBoundingClientRect" ||
                  keyName == "clientRects.rangeClientRects" ||
                  keyName == "clientRects.rangeBoundingClientRect")
                {
                  List<string> cache = GetCachedValue(valueCache, "_clientRect", value);
                  value = $"#{cache.IndexOf(value)} clientRect";
                }
                //wsData.Cells[dataRow, dataCol].Value = value;

                wsList.Cells[listRow, 1].Value = item.Caption;
                wsList.Cells[listRow, 2].Value = item.DeviceName;
                wsList.Cells[listRow, 3].Value = keyName;
                wsList.Cells[listRow, 4].Value = value;
                wsList.Cells[listRow, 5].Value = item.Folder;
                listRow++;
              }
            }
            else
            {
              //wsData.Cells[dataRow, dataCol].Value = " ";
            }
            dataRow++;
          }
          dataCol++;
        }

        await pck.SaveAsAsync(output, cancellationToken);
      }

      result.Elements
        .SelectMany(x => x.Elements)
        .ToList()
        .ForEach(x =>
        {
          x.Key = result.Keys.IndexOf(x.Key).ToString();
        });

      result.Elements
        .SelectMany(x => x.Elements)
        .SelectMany(x => x.ValueList)
        .ToList()
        .ForEach(x =>
        {
          x.DevicesName =
            [.. x.DevicesName.Select(x => result.DevicesName.IndexOf(x).ToString())];
        });

      string jsonOut = JsonSerializer.Serialize(result);
      await File.WriteAllTextAsync(
        Path.ChangeExtension(output, ".json"),
        jsonOut, cancellationToken);

      return 0;
    }

    private static void AddKeyGroup1(OfficeOpenXml.ExcelPackage pck)
    {
      var rows = new List<string>()
      {
        "canvas2d.$hash",
        "screen.height",
        "screen.width",
        "screen.pixelDepth",
        "navigator.platform",
        "canvas2d.dataURI",
        "canvas2d.paintURI",
        "canvas2d.textURI",
        "canvas2d.emojiURI",
        "canvas2d.textMetricsSystemSum",
        "canvas2d.emojiSet",
        "canvasWebgl.pixels",
        "canvasWebgl.pixels2",
        "canvasWebgl.dataURI",
        "canvasWebgl.dataURI2",
        "cssMedia.mediaCSS.device-aspect-ratio",
        "cssMedia.mediaCSS.device-screen",
        "cssMedia.$hash",
        "screen.availWidth",
        "screen.availHeight",
        "clientRects.$hash",
        "clientRects.domrectSystemSum",
        "fonts.pixelSizeSystemSum",
        "displayDensity",
        "realDisplaySize",
        "clientRects.emojiSet",
        "fonts.$hash",
        "cssMedia.anyPointer",
        "cssMedia.mediaCSS.color-gamut",
        "cssMedia.mediaCSS.orientation",
      };

      var wsData = pck.Workbook.Worksheets.Add("keygroup1");
      wsData.Cells[1, 1].Value = "Key";
      wsData.Cells[1, 2].Value = "KeyGroup1";

      int listRow = 2;
      foreach (var row in rows)
      {
        wsData.Cells[listRow, 1].Value = row;
        wsData.Cells[listRow, 2].Value = true;
        listRow++;
      }
    }

    private static List<string> GetCachedValue(Dictionary<string, List<string>> hashCache, string keyName, string value)
    {
      if (!hashCache.TryGetValue(keyName, out var cache))
      {
        cache = [];
        hashCache.Add(keyName, cache);
      }
      if (!cache.Contains(value))
        cache.Add(value);
      return cache;
    }

    private static Elements FindBy(List<Elements> list, string keyName, string device)
    {
      foreach (var element in list)
      {
        if (element.Key == keyName)
        {
          foreach (var value in element.ValueList)
          {
            if (value.DevicesName.Contains(device))
              return element;
          }
        }
      }
      return null;
    }

    private static async Task<int> ProcessFiles(
      string[] allfiles, List<Elements> elements,
      CancellationToken cancellationToken)
    {
      foreach (string file in allfiles)
      {
        if (!File.Exists(file)) continue;

        var deviceName = Path.GetDirectoryName(file).Split(Path.DirectorySeparatorChar).Last();
        Console.WriteLine($"Parsing {file}");

        var json = await File.ReadAllTextAsync(file, cancellationToken);
        var json_Dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        foreach (var key in json_Dictionary.Keys)
        {
          ParseDictionary(elements, deviceName, null, key, json_Dictionary);
        }
      }
      return 0;
    }

    private static void ParseDictionary(List<Elements> elements,
      string deviceName,
      string parentKey, string key,
      Dictionary<string, JsonElement> root)
    {
      var currentKey = $"{(parentKey != null ? parentKey + "." : "")}{key}";
      var v = root.GetValueOrDefault(key);

      if (v.ValueKind == JsonValueKind.Undefined)
        throw new NotSupportedException();

      string value = v.GetRawText().Trim();
      if (v.ValueKind == JsonValueKind.Object)
      {
        try
        {
          var json_Dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);

          foreach (var item in json_Dictionary.Keys)
          {
            ParseDictionary(elements, deviceName,
              currentKey, item,
              json_Dictionary);
          }
        }
        catch (Exception)
        {
          // no matter
        }
      }
      else if (v.ValueKind == JsonValueKind.Array)
      {
        var array = v.EnumerateArray().ToList();
        var valueString = string.Join(',',
            array.Select(x => x.GetRawText()).ToList());

        AddElement(elements,
          deviceName,
          currentKey,
          valueString);
      }
      else
      {
        AddElement(elements, deviceName, currentKey, value);
      }
    }

    private static void AddElement(List<Elements> elements,
      string deviceName, string parentKey, string value)
    {
      var element = elements.FirstOrDefault(x => x.Key == parentKey);
      if (element == null)
      {
        element = new Elements()
        {
          Key = parentKey,
          ValueList = []
        };
        elements.Add(element);
      }

      var elementValue =
        element.ValueList.FirstOrDefault(x => x.Value == value);

      if (elementValue == null)
      {
        elementValue = new ElementValue()
        {
          Value = value,
          DevicesName = []
        };
        element.ValueList.Add(elementValue);
      }

      if (!elementValue.DevicesName.Contains(deviceName))
        elementValue.DevicesName.Add(deviceName);
    }
  }
}
