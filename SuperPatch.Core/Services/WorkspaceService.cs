using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

using SuperPatch.Core;
using DiffPatch.Data;
using SuperPatch.Core.Status;

namespace SuperPatch.Core.Services
{
  public enum RepoType
  {
    Bromite,
    Kiwi
  }

  public class WorkspaceService
  {
    HttpClient _http;
    Core.GitHubApi.ApiService _githubApi;

    public WorkspaceService(
      HttpClient http, 
      Core.GitHubApi.ApiService githubApi)
    {
      _http = http;
      _githubApi = githubApi;
    }

    public async Task<ApiResult<Workspace>> LoadBySha(RepoType repoType, string sha)
    {
      var ret = new ApiResult<Workspace>();

      var wrk = new Workspace()
      {
        CommitShaOrTag = sha,
      };

      if (repoType == RepoType.Bromite)
        wrk.Storage =
          new SuperPatch.Core.Storages.Bromite.BromiteRemoteStorage(wrk, _http);
      else if (repoType == RepoType.Kiwi)
        wrk.Storage =
          new SuperPatch.Core.Storages.Kiwi.KiwiRemoteStorage(wrk, _http, _githubApi);
      else
        throw new ApplicationException("unkwown type");

      // Check sha
      try
      {
        await wrk.EnsureLoadPatchesOrderAsync();
      }
      catch
      {
        return ret.WithMessage($"Invalid sha '{sha}' value");
      }

      return ret.WithResult(wrk).WithOk();
    }

    public async Task LoadIfPullRequest(Workspace wrk)
    {
      // Check if sha is a pull request
      var ret = await _githubApi.CommitForSha(wrk, wrk.CommitShaOrTag);
      wrk.CommitMessage = ret?.commit?.message;

      if (ret?.commit != null)
      {
        var commit = await _githubApi.Search(wrk.CommitShaOrTag);
        if (commit != null &&
            commit.items.Count() > 0 &&
            commit.items[0].pull_request != null)
        {
          // yes, it is
          var pullUrl = commit.items[0].pull_request.url;
          var commits = await _githubApi.GetCommitsForPull(pullUrl);

          // load all commits from pull request
          wrk.RelatedCommits = commits.Select(x => new Workspace()
          {
            CommitShaOrTag = x.sha,
            CommitMessage = x.commit.message
          }).ToList();

          wrk.RelatedCommits.AddRange( commits.Where(x=>x.parents != null)
                        .SelectMany( x => x.parents )
                        .Select( x => new Workspace()
          {
              CommitShaOrTag = "bd8369329286eb256cb4c835d0abee0251c234e3",
              CommitMessage = $"x.sha"
          }));
          wrk.RelatedCommits.ForEach(x => x.Storage = wrk.Storage.Clone(x));
          
          
        }
      }
    }
  }
}
