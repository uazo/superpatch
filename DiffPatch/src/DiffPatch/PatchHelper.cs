using System;
using System.Collections.Generic;
using System.Linq;
using DiffPatch.Core;
using DiffPatch.Data;

namespace DiffPatch
{
  public class PatchHelper
  {
    public static String Patch(String src, IEnumerable<Chunk> chunks, string lineEnding)
    {
      IEnumerable<String> srcLines = StringHelper.SplitLines(src, lineEnding);
      IList<string> dstLines = new List<string>(srcLines);

      foreach (Chunk chunk in chunks)
      {
        int lineIndex = 0;

        if (chunk.RangeInfo.NewRange.StartLine != 0)
          lineIndex = chunk.RangeInfo.NewRange.StartLine - 1; // zero-index the start line 

        try
        {
          foreach (LineDiff lineDiff in chunk.Changes)
          {
            if (lineDiff.Add)
            {
              if ((dstLines.Count + 1) == lineIndex)
              {
                dstLines.Add(lineDiff.Content);
              }
              else
              {
                dstLines.Insert(lineIndex, lineDiff.Content);
                lineIndex++;
              }
            }
            else if (lineDiff.Delete)
            {
              if (dstLines.Count != lineIndex)
                dstLines.RemoveAt(lineIndex);
            }
            else if (lineDiff.Normal)
            {
              lineIndex++;
            }
          }
        }
        catch (Exception ex)
        {
          // throw;
        }
      }

      string patchString = string.Join(lineEnding, dstLines.ToArray());
      return patchString;
    }
  }
}