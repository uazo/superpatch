using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperPatch.Models
{
  public class CommonUIData
  {
    private CommonUIData() { }

    public static CommonUIData GetNew() => new CommonUIData();

    public CommonUIData Clone()
    {
      return new CommonUIData()
      {
        Width = Width,
        Height = Height
      };
    }

    public int Width { get; set; }
    public int Height { get; set; }
  }
}
