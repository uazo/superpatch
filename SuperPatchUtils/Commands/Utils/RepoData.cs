using System.Collections.Generic;

using DiffPatch.Data;
using SuperPatch.Core;

namespace SuperPatchUtils.Commands.Utils
{
  public class RepoData
  {
    public Workspace Workspace { get; set; }
    public List<IFileDiff> Files { get; internal set; }
  }
}
