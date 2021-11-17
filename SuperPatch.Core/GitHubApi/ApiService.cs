using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.GitHubApi
{
  public class ApiService
  {
    private HttpClient httpClient { get; set; }

    public ApiService(HttpClient httpClient)
    {
      this.httpClient = httpClient;
    }

    public async Task<CommitResponse> CommitForSha(Workspace wrk, string sha)
    {
      try
      {
        var result = await httpClient.GetFromJsonAsync<CommitResponse>(
          $"{wrk.Storage.GitHubApiEndpoint}/commits/{sha}");
        return result;
      }
      catch
      {
        // TODO:
        // 422 (Unprocessable Entity)
        return null;
      }
    }

    public async Task<SearchResponse> Search(string sha)
    {
      try
      {
        // TODO: Fix me
        var result = await httpClient.GetFromJsonAsync<SearchResponse>(
          $"https://api.github.com/search/issues?q={sha}");
        return result;
      }
      catch
      {
        // TODO:
        // 422 (Unprocessable Entity)
        return null;
      }
    }

    public async Task<PullRequest> GetPullRequest(string repo, int pullId)
    {
      try
      {
        var result = await httpClient.GetFromJsonAsync<PullRequest>(
          $"https://api.github.com/repos/{repo}/pulls/{pullId}");
        return result;
      }
      catch
      {
        // TODO:
        // 422(Unprocessable Entity)
        return null;
      }
    }

    public async Task<List<PullRequestFile>> GetPullRequestFiles(string repo, int pullId)
    {
      try
      {
        var result = await httpClient.GetFromJsonAsync<List<PullRequestFile>>(
          $"https://api.github.com/repos/{repo}/pulls/{pullId}/files");
        return result;
      }
      catch
      {
        // TODO:
        // 422(Unprocessable Entity)
        return null;
      }
    }

    public async Task<PullRequestFileContents> GetPullRequestFiles(PullRequestFile file)
    {
      try
      {
        var result = await httpClient.GetFromJsonAsync<PullRequestFileContents>(file.contents_url);
        return result;
      }
      catch
      {
        // TODO:
        // 422(Unprocessable Entity)
        return null;
      }
    }

    public async Task<List<PullRequestCommitResponse>> GetCommitsForPull(string pullurl)
    {
      try
      {
        var result = await httpClient.GetFromJsonAsync<List<PullRequestCommitResponse>>(
          $"{pullurl}/commits");
        return result;
      }
      catch
      {
        // TODO:
        // 422(Unprocessable Entity)
        return null;
      }
    }
    
  }
}
