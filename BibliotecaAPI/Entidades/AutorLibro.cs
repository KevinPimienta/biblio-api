using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    [PrimaryKey(nameof(AutorId), nameof(LibroId))]
    public class AutorLibro
    {
        public int AutorId { get; set; }
        public int LibroId { get; set; }
        public int Orden { get; set; }
        public Autor? Autor { get; set; } //Prpiedad de navegacion
        public Libro? Libro { get; set; } //Prpiedad de navegacion

    }
}
