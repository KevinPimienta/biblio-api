using BibliotecaAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class AutorCreacionDTO
    {
        //private object value1;
        //private object value2;
        //private object value3;

        //public AutorCreacionDTO(object value1, object value2, object value3)
        //{
        //    this.value1 = value1;
        //    this.value2 = value2;
        //    this.value3 = value3;
        //}

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraMayuscula]
        public required string Nombres { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        [PrimeraMayuscula]
        public required string Apellidos { get; set; }
        [StringLength(20, ErrorMessage = "El campo {0} debe tener {1} caracteres o menos")]
        public string? Identificacion { get; set; }
        //public string? Indetificacion { get; set; }
        public List<LibroCreacionDTO> Libros { get; set; } = [];
        
        //AutorCreacionDTO, AutorPatch, Autor = Indetificaion
    }
}
