using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace SuperPatch.Models
{
  public class Node<T>
  {
    public T Item { get; set; }
    
    public string Label { get; set; }

    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public string Key { get; set; }

    public Nodes<T> Childs { get; set; } = new Nodes<T>();
  }

  public class Nodes<T> : List<Node<T>>
  {
    public Node<T> GetOrCreateNode(IEnumerable<string> paths)
    {
      var parent = this;
      Node<T> item = null;
      string key = null;
      foreach (var path in paths)
      {
        key = $"{key}_{path}";
        var node = parent.FirstOrDefault(x => x.Label == path);
        if (node == null)
        {
          node = new Node<T>() { Label = path };
          node.Key = key;
          parent.Add(node);
        }
        item = node;
        parent = node.Childs;
      }
      return item;
    }

    public IEnumerable<Node<T>> Flatten()
    {
      foreach (var node in this)
      {
        yield return node;

        if (node.Childs == null)
          yield break;

        foreach (var child in node.Childs.Flatten())
          yield return child;
      }
    }
  }
}
