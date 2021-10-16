using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperPatch.Models;

namespace SuperPatch.Components
{
  public class CommonComponentBase: ComponentBase
  {
    [CascadingParameter(Name = "CommonData")] 
    protected CommonUIData CommonData { get; private set; }

    [Inject] protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected NavigationManager navigationManager { get; set; }

    public Action StateHasChangedAsync { get; set; }

    public CommonComponentBase()
    {
      this.StateHasChangedAsync = RecalcUIAsync;
    }

    private async void RecalcUIAsync()
    {
      await InvokeAsync(() => StateHasChanged());
    }

    protected async Task NavigateToNewTabAsync(string url)
    {
      try
      {
        await JSRuntime.InvokeAsync<bool>("window.location.replace", url);
      }
      catch
      {
      }
    }

    protected void ForceReloadPage()
    {
      navigationManager.NavigateTo(navigationManager.Uri, true);
    }
  }
}
