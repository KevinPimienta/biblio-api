using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.ServiciosUsuarios
{
    public class ServiciosUsuarios : IServiciosUsuarios
    {   
        //Principio de inversion de dependencia - Tipos abstractos y tipos concretos
        //Por que esto es un servicio? Que hizo la interfaz creada?
        //Por que poner el servicio en Program.cs?
        private readonly UserManager<Usuario> userManager;
        private readonly IHttpContextAccessor contextAccessor;

        public ServiciosUsuarios(UserManager<Usuario> userManager, IHttpContextAccessor contextAccessor)
        {
            this.userManager = userManager;
            this.contextAccessor = contextAccessor;
        }

        public async Task<Usuario?> ObtenerUsuario()
        {
            var emailClaim = contextAccessor.HttpContext!.User.Claims.Where(x => x.Type == "email").FirstOrDefault();

            if (emailClaim is null)
            {
                return null;
            }

            var email = emailClaim.Value;
            return await userManager.FindByEmailAsync(email);

        }

    }
}
