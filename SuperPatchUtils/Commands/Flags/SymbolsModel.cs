using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperPatchUtils.Commands.Flags
{
  public class SymbolsModel
  {
    [NotMapped] public string Id { get; set; }
    [Column("Name", TypeName = "varchar")] public string Name { get; set; }
    [Column("Scope", TypeName = "varchar")] public string Scope { get; set; }
    [Column("Signature", TypeName = "varchar")] public string Signature { get; set; }
    [Column("Documentation", TypeName = "varchar")] public string Documentation { get; set; }
    [Column("ReturnType", TypeName = "varchar")] public string ReturnType { get; set; }
    [Column("Type", TypeName = "varchar")] public string Type { get; set; }
    [Column("DefPath", TypeName = "varchar")] public string DefPath { get; set; }
    [Column("DefStartLine", TypeName = "int")] public int DefStartLine { get; set; }
    [Column("DefStartCol", TypeName = "int")] public int DefStartCol { get; set; }
    [Column("DefEndLine", TypeName = "int")] public int DefEndLine { get; set; }
    [Column("DefEndCol", TypeName = "int")] public int DefEndCol { get; set; }
    [Column("DeclPath", TypeName = "varchar")] public string DeclPath { get; set; }
    [Column("DeclStartLine", TypeName = "int")] public int DeclStartLine { get; set; }
    [Column("DeclStartCol", TypeName = "int")] public int DeclStartCol { get; set; }
    [Column("DeclEndLine", TypeName = "int")] public int DeclEndLine { get; set; }
    [Column("DeclEndCol", TypeName = "int")] public int DeclEndCol { get; set; }
    [Column("Kind", TypeName = "int")] public int Kind { get; set; }
    [Column("SubKind", TypeName = "int")] public int SubKind { get; set; }
    [Column("Language", TypeName = "int")] public int Language { get; set; }
    [Column("Generic", TypeName = "int")] public int Generic { get; set; }
    [Column("TemplatePartialSpecialization", TypeName = "int")] public int TemplatePartialSpecialization { get; set; }
    [Column("TemplateSpecialization", TypeName = "int")] public int TemplateSpecialization { get; set; }
    [Column("UnitTest", TypeName = "int")] public int UnitTest { get; set; }
    [Column("IBAnnotated", TypeName = "int")] public int IBAnnotated { get; set; }
    [Column("IBOutletCollection", TypeName = "int")] public int IBOutletCollection { get; set; }
    [Column("GKInspectable", TypeName = "int")] public int GKInspectable { get; set; }
    [Column("Local", TypeName = "int")] public int Local { get; set; }
    [Column("ProtocolInterface", TypeName = "int")] public int ProtocolInterface { get; set; }

    [Column("DefaultValue", TypeName = "int")]
    [IgnoreLoader] public string DefaultValue { get; set; }

    [Column("CromiteValue", TypeName = "int")]
    [IgnoreLoader] public string BromiteValue { get; set; }

    [Column("AboutFlagName", TypeName = "int")]
    [IgnoreLoader] public string AboutFlagName { get; internal set; }

    [Column("AboutFlagDescription", TypeName = "int")]
    [IgnoreLoader] public string AboutFlagDescription { get; internal set; }

    [Column("AboutFlagOs", TypeName = "int")]
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
    [JsonIgnore] public List<SymbolsModel> FlagList { get; set; }
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

  [AttributeUsage(AttributeTargets.Property)]
  public class IgnoreLoaderAttribute : Attribute { }
}
