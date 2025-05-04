using System.Linq;
using DiffPatch.Data;

namespace SuperPatch.Core.Utils
{
  public static class FileDiffExtensions
  {
    public static bool IsEqualTo(this FileDiff me, FileDiff other)
    {
      if (other == null) return false;
      if (me.Chunks.Count != other.Chunks.Count) return false;

      foreach (var chunk in me.Chunks)
      {
        var otherChunk = other.Chunks
          .FirstOrDefault(
            x => x.RangeInfo.OriginalRange.StartLine == chunk.RangeInfo.OriginalRange.StartLine);

        if (otherChunk == null) return false;
        if (otherChunk.Content != chunk.Content) return false;
      }

      return true;
    }
  }
}
