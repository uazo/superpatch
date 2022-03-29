using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperPatchUtils.Commands.Flags
{
  public class SymbolsModel
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Scope { get; set; }
    public string Signature { get; set; }
    public string Documentation { get; set; }
    public string ReturnType { get; set; }
    public string Type { get; set; }
    public string DefPath { get; set; }
    public int DefStartLine { get; set; }
    public int DefStartCol { get; set; }
    public int DefEndLine { get; set; }
    public int DefEndCol { get; set; }
    public string DeclPath { get; set; }
    public int DeclStartLine { get; set; }
    public int DeclStartCol { get; set; }
    public int DeclEndLine { get; set; }
    public int DeclEndCol { get; set; }
    public int Kind { get; set; }
    public int SubKind { get; set; }
    public int Language { get; set; }
    public int Generic { get; set; }
    public int TemplatePartialSpecialization { get; set; }
    public int TemplateSpecialization { get; set; }
    public int UnitTest { get; set; }
    public int IBAnnotated { get; set; }
    public int IBOutletCollection { get; set; }
    public int GKInspectable { get; set; }
    public int Local { get; set; }
    public int ProtocolInterface { get; set; }

    [IgnoreLoader] public string DefaultValue { get; set; }
    [IgnoreLoader] public string BromiteValue { get; set; }
    [IgnoreLoader] public string AboutFlagName { get; internal set; }
    [IgnoreLoader] public string AboutFlagDescription { get; internal set; }
    [IgnoreLoader] public string AboutFlagOs { get; internal set; }

    public string GetFlagOrScopeName()
    {
      if (AboutFlagName == null)
        return $"{Scope}{Name}";
      else
        return AboutFlagName;
    }
  }

  public enum SymbolStatus
  {
    None = 0,
    Added = 1,
    Removed = 2,
  }

  public class SymbolVersion
  {
    public int Version { get; set; }
    public SymbolsModel Symbol { get; set; }
    public SymbolStatus Status { get; set; }
  }

  public class Version
  {
    public int Id { get; set; }
    public string Build { get; set; }
  }

  public class Flag
  {
    public string Name { get; set; }
    public List<SymbolVersion> Commits { get; set; }
    public int FirstSeenAt { get; set; }
  }

  public class ConsolidateList
  {
    public List<Flag> Symbols { get; set; }
    public List<Version> Versions { get; set; }
  }

  public class IgnoreLoaderAttribute : Attribute { }
}
