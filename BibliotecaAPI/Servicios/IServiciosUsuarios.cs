using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.ServiciosUsuarios
{
    public interface IServiciosUsuarios
    {
        Task<Usuario?> ObtenerUsuario();
    }
}