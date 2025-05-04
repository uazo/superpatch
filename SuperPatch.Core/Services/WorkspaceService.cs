using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SuperPatch.Core.Services
{
  public enum RepoType
  {
    Bromite,
    Kiwi
  }

  public class WorkspaceService(
    HttpClient http,
    GitHubApi.ApiService githubApi)
  {
    public async Task<ApiResult<Workspace>> LoadBySha(RepoType repoType, string sha)
    {
      var ret = new ApiResult<Workspace>();

      var wrk = new Workspace()
      {
        CommitShaOrTag = sha,
      };

      if (repoType == RepoType.Bromite)
        wrk.Storage =
          new Storages.Bromite.BromiteRemoteStorage(wrk, http);
      else if (repoType == RepoType.Kiwi)
        wrk.Storage =
          new Storages.Kiwi.KiwiRemoteStorage(wrk, http, githubApi);
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
      var ret = await githubApi.CommitForSha(wrk, wrk.CommitShaOrTag);
      wrk.CommitMessage = ret?.commit?.message;

      if (ret?.commit != null)
      {
        var commit = await githubApi.Search(wrk.CommitShaOrTag);
        if (commit != null &&
            commit.items.Length > 0 &&
            commit.items[0].pull_request != null)
        {
          // yes, it is
          var pullUrl = commit.items[0].pull_request.url;
          var commits = await githubApi.GetCommitsForPull(pullUrl);

          // load all commits from pull request
          wrk.RelatedCommits = [.. commits.Select(x => new Workspace()
          {
            CommitShaOrTag = x.sha,
            CommitMessage = x.commit.message
          })];

          wrk.RelatedCommits.AddRange(commits.Where(x => x.parents != null)
                        .SelectMany(x => x.parents)
                        .Select(x => new Workspace()
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
