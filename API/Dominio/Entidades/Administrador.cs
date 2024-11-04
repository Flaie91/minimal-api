using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinimalApi.Dominio.Entidades;

public class Administrador
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; } = default!;

  [Required]
  [StringLength(255)]
  public String Email { get; set; } = default!;

  [Required]
  [StringLength(50)]
  public String Senha { get; set; } = default!;

  [Required]
  [StringLength(10)]
  public String Perfil { get; set; } = default!;
}