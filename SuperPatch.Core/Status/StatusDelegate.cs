using System;
using System.Threading.Tasks;

namespace SuperPatch.Core.Status
{
  public class NoopStatusDelegate : StatusDelegate
  {
  }

  public class StatusDelegate
  {
    public Func<string, Task> OnChangedAsync { get; set; }
    public Func<string, Task> OnBeginWorkAsync { get; set; }
    public Func<string, Task> OnEndWorkAsync { get; set; }

    public class WorkRef
    {
      private readonly StatusDelegate _parent;

      internal WorkRef(StatusDelegate parent)
      {
        _parent = parent;
      }

      public async Task EndWork(string msg = null)
      {
        if (_parent.OnEndWorkAsync != null)
          await _parent.OnEndWorkAsync(msg);
      }
    }

    public async Task InvokeAsync(string msg)
    {
      if (OnChangedAsync != null)
        await OnChangedAsync(msg);
    }

    public async Task<WorkRef> BeginWork(string msg = null)
    {
      if (OnBeginWorkAsync != null)
        await OnBeginWorkAsync(msg);
      return new WorkRef(this);
    }
  }
}
