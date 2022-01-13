using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.Storages.Bromite
{
  public abstract class BromiteStorage : ChromiumStorage
  {
    public BromiteStorage(Workspace wrk, HttpClient http) : base(wrk, http) { }

    public override string GitHubApiEndpoint => @"https://api.github.com/repos/bromite/bromite";
    public override string LogoUrl => @"https://www.bromite.org/bromite.png";
  }
}
