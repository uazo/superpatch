using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
