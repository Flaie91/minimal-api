using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinimalApi.Dominio.Entidades;

public class Veiculo
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; } = default!;

  [Required]
  [StringLength(150)]
  public String Nome { get; set; } = default!;

  [Required]
  [StringLength(100)]
  public String Marca { get; set; } = default!;
  
  [Required]
  public int Ano { get; set; } = default!;
}