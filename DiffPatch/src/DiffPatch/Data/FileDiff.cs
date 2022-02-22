using System.Collections.Generic;
using System.Linq;

namespace DiffPatch.Data
{
  public interface IFileDiff
  {
    bool Add { get; }
    int Additions { get; set; }
    List<Chunk> Chunks { get; set; }
    bool Deleted { get; }
    int Deletions { get; set; }
    string From { get; set; }
    List<string> Index { get; set; }
    string To { get; set; }
    FileChangeType Type { get; set; }
  }

  public class FileDiff : IFileDiff
  {
    public List<Chunk> Chunks { get; set; } = new List<Chunk>();

    public int Deletions { get; set; }
    public int Additions { get; set; }

    public string To { get; set; }

    public string From { get; set; }

    public FileChangeType Type { get; set; }

    public bool Deleted => Type == FileChangeType.Delete;

    public bool Add => Type == FileChangeType.Add;

    public List<string> Index { get; set; }
  }
}
