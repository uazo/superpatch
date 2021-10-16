using System;
using System.Collections.Generic;
using System.Text;

namespace DiffPatch.Data
{
  public class ChunkRangeInfo
  {
    public ChunkRangeInfo()
    {
    }

    public ChunkRangeInfo(ChunkRange originalRange, ChunkRange newRange)
    {
      OriginalRange = originalRange;
      NewRange = newRange;
    }

    public ChunkRange OriginalRange { get; set; }

    public ChunkRange NewRange { get; set; }
  }
}