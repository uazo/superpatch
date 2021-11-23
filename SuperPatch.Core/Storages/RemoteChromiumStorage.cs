using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DiffPatch.Data;

namespace SuperPatch.Core.Storages
{
  public abstract class RemoteChromiumStorage : Storage
  {
    protected HttpClient http { get; private set; }

    protected virtual string FileSourceUrl => @"https://raw.githubusercontent.com/chromium/chromium";

    protected string ChromiumCommit { get; set; }

    public RemoteChromiumStorage(Workspace wrk, HttpClient http) : base(wrk)
    {
      this.http = http;
    }

    protected virtual async Task FetchChromiumCommit()
    {
      ChromiumCommit = workspace.CommitShaOrTag;
      await Task.CompletedTask;
    }

    public override async Task<string> GetFileAsync(FileDiff file)
    {
      if (file.From == "/dev/null") return string.Empty;
      if (string.IsNullOrEmpty(ChromiumCommit)) await FetchChromiumCommit();
      return await http.GetStringAsync($"{FileSourceUrl}/{ChromiumCommit}/{file.From}");
    }
  }
}
