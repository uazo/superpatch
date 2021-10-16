using System;
using System.Collections.Generic;
using System.Text;

namespace DiffPatch.Data
{
  /// <summary>
  /// Represents the +/-s,c in a ChunkRange header (@@ -s,c +s,c @@)
  /// </summary>
  public class ChunkRange
  {
    public ChunkRange()
    {
    }

    public ChunkRange(int startLine, int lineCount)
    {
      StartLine = startLine;
      LineCount = lineCount;
    }

    /// <summary>
    /// First line of change. Non-zero indexed, first line is 1.
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Number of lines affected in the change. Non-zero indexed, one line addition equals 1 in this property.
    /// </summary>
    public int LineCount { get; set; }
  }
}