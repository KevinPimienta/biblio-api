using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPITest.Utilidades;
using System.Net;
using System.Text.Json;

namespace BibliotecaAPITest.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas: BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            var status = respuesta.StatusCode;
            Assert.AreEqual(expected:HttpStatusCode.NotFound, actual:respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres = "Felipe", Apellidos = "Gavilan" });
            context.Autores.Add(new Autor() { Nombres = "Felipe", Apellidos = "Gavilan" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            respuesta.EnsureSuccessStatusCode();

            var autor = JsonSerializer.Deserialize<AutorConLibrosDTOs>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, autor.Id);
        }

        [TestMethod]
        public async Task Post_Devuelve401_CuandoUsuarioNoEstaAutenticado()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO { Nombres = "Felipe", Apellidos = "Gavilan", Identificacion = "123"};

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }


        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioNoEsAdmin()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var token = await CrearUsuario(nombreBD, factory);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO { Nombres = "Felipe", Apellidos = "Gavilan", Identificacion = "123" };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

    }
}
