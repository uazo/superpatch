using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperPatch.Core.Server.Infrastructure.Models
{
  [Table("Version")]
  public class Version
  {
    [Key]
    [Column("VersionID", TypeName = "int")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int VersionID { get; set; }

    [Column("Build", TypeName = "varchar")]
    [MaxLength(20)]
    public string? Build { get; set; }
  }
}
