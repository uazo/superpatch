using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.Storages.Kiwi
{
  public abstract class KiwiStorage : ChromiumStorage
  {
    public KiwiStorage(Workspace wrk, HttpClient http) : base(wrk, http) { }

    protected string KiwiRepoName => "uazo/kiwi-as-a-patch";

    public override string GitHubApiEndpoint => $"https://api.github.com/repos/{KiwiRepoName}";
    public override string LogoUrl => @"https://avatars.githubusercontent.com/u/40272275?v=4";
  }
}
