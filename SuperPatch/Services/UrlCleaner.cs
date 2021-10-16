using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperPatch.Services
{
  // TODO: fix me
  public class UrlCleaner
  {
    public string CleanForUrl(string url) => url?.Replace("/", "=")
                                                 .Replace(".", "*");

    public string RestoreFromUrl(string url) => url?.Replace("=", "/")
                                                    .Replace("*", ".");
  }
}
