using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperPatch.Models
{
  public class Node<T>
  {
    public T Item { get; set; }
    
    public string Label { get; set; }

    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    public Nodes<T> Childs { get; set; } = new Nodes<T>();
  }

  public class Nodes<T> : List<Node<T>>
  {
    public Node<T> GetOrCreateNode(IEnumerable<string> paths)
    {
      var parent = this;
      Node<T> item = null;
      foreach (var path in paths)
      {
        var node = parent.FirstOrDefault(x => x.Label == path);
        if (node == null)
        {
          node = new Node<T>() { Label = path };
          parent.Add(node);
        }
        item = node;
        parent = node.Childs;
      }
      return item;
    }
  }
}
