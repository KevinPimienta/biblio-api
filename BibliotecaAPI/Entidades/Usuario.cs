using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Entidades
{
    public class Usuario: IdentityUser //Por que eredar de IdentityUser? Luego se tuvo que remplazar todos lo Identity en la solucion
    {
        public DateTime FechaNacimeinto { get; set; }
    }
}
