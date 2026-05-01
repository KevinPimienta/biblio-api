using BibliotecaAPI.Entidades;
using BibliotecaAPI.ServiciosUsuarios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITest.PruebasUnitarias.Controllers.Servicios
{
    [TestClass]
    public  class ServiciosUsuariosPrueba
    {
        private UserManager<Usuario> userManager = null!;
        private IHttpContextAccessor contextAccessor = null!;
        private ServiciosUsuarios serviciosUsuarios = null!;

        [TestInitialize]
        public void Setup()
        {
            userManager = Substitute.For<UserManager<Usuario>>(Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            contextAccessor = Substitute.For<IHttpContextAccessor>();
            serviciosUsuarios = new ServiciosUsuarios(userManager, contextAccessor);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoNoHayClaimEmial()
        {
            // Preparacion
            var httpsContext = new DefaultHttpContext();
            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario()
;            // Verificacion
            Assert.IsNull(usuario);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoHayClaimEmail()
        {
            // Preparacion
            var email = "prueba@gmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            
            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(usuarioEsperado));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim("email", email)
                }));

            var httpsContext = new DefaultHttpContext() { User = claims };
            contextAccessor.HttpContext.Returns(httpsContext);
            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario()
;            // Verificacion
            Assert.IsNotNull(usuario);
            Assert.AreEqual(expected: email, actual: usuario.Email);
        }


        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoUsuarioNoExiste()
        {
            // Preparacion
            var email = "prueba@gmail.com";
            var usuarioEsperado = new Usuario { Email = email };


            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<Usuario>(null!));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim("email", email)
                }));

            var httpsContext = new DefaultHttpContext() { User = claims };
            contextAccessor.HttpContext.Returns(httpsContext);
            // Prueba
            var usuario = await serviciosUsuarios.ObtenerUsuario()
;            // Verificacion
            Assert.IsNull(usuario);
            
        }
    }
}
