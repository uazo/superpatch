using System.Collections.Generic;

namespace DiffPatch.Data
{
  public class Chunk
  {
    public Chunk() { }

    public Chunk(string content, ChunkRangeInfo rangeInfo)
    {
      Content = content;
      RangeInfo = rangeInfo;
      Changes = new List<LineDiff>();
    }

    public List<LineDiff> Changes { get; set; }

    public string Content { get; set; }

    public ChunkRangeInfo RangeInfo { get; set; }
  }
}