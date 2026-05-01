using BibliotecaAPI.DTOs;
using System.ComponentModel.DataAnnotations;
namespace BibliotecaAPI.Entidades;

public class Libro
{
    public int Id { get; set; }
    [Required]
    [StringLength(250, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
    public required string Titulo { get; set; }
    public List<AutorLibro> Autores { get; init; } = [];
    public List<Comentario> Comentarios { get; set; } = [];
}
