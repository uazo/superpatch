using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SuperPatch.Core;

namespace SuperPatch.Models
{
  public class CommitViewerModel
  {
    public Workspace Workspace { get; set; }
    
    public PatchView CurrentPatchView { get; set; }

    public Workspace CompareWith { get; set; }
    public PatchView CompareWithPatchView { get; set; }

    public FilePatchedContents File { get; set; }
    public FilePatchedContents CurrentFile { get; set; }

    public Node<FilePatchedContents> RootFolder { get; set; }

    public IEnumerable<PatchFile> CurrentSelectedPatches { get; set; }

    public Core.Status.StatusDelegate StatusUpdater { get; set; }

    public string[] TreeExpandedKeys { get; set; }

    public int LeftPanelWidth = 300;
  }
}
