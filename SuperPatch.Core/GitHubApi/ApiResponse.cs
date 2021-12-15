using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatch.Core.GitHubApi
{
  // response for
  // https://api.github.com/repos/bromite/bromite/commits/{commitid}
  public class CommitResponse 
  {
    public string sha { get; set; }
    public string node_id { get; set; }
    public string html_url { get; set; }

    public Commit commit { get; set; }
  }
  public class Commit
  {
    public string message { get; set; }
    public Tree tree { get; set; }

  }

  public class Tree
  {
    public string sha { get; set; }
    public string url { get; set; }
    public TreeItem[] tree { get; set; }
  }

  public class TreeItem
  {
    public string path { get; set; }
    public string type { get; set; } // tree, blob
    public string sha { get; set; }
    public string url { get; set; }

    internal bool isTree() => type == "tree";
    internal bool isBlob() => type == "blob";
  }

  // response for
  // https://api.github.com/search/issues?q={commitid}
  public class SearchResponse
  {
    public int total_count { get; set; }
    public bool incomplete_results { get; set; }

    public SearchResponseItem[] items {get;set;}
  }

  public class SearchResponseItem
  {
    public string url { get; set; }
    public string title { get; set; }

    public PullRequest pull_request { get; set; }
  }

  public class Head
  {
    public string sha { get; set; }
  }

  public class PullRequestFile
  {
    public string sha { get; set; }
    public string filename { get; set; }
    public string contents_url { get; set; }
  }

  public class PullRequestFileContents
  {
    public string sha { get; set; }
    public string name { get; set; }
    public string path { get; set; }
    public string download_url { get; set; }
  }

  public class PullRequest
  {
    public string url { get; set; }
    public string title { get; set; }
    public string html_url { get; set; }
    public string diff_url { get; set; }
    public string patch_url { get; set; }
    public string merge_commit_sha { get; set; }
    
    public Head head { get; set; }
  }

  // response
  // for https://api.github.com/repos/bromite/bromite/pulls/{pullid}/commits
  public class PullRequestCommitResponse
  {
    public string sha { get; set; }
    public Commit commit { get; set; }
  }
}
