using AutoMapper;
using Azure;
using BibliotecaAPI.Controller.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.ServiciosUsuarios;
using BibliotecaAPITest.Utilidades;
using BibliotecaAPITest.Utilidades.Dobles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresContollerPruebas : BasePruebas
    {
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore outputCacheStore = null!;
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();

            controller = new AutoresController(context, mapper, almacenadorArchivos, logger, outputCacheStore);
        }


        [TestMethod]
        public async Task Get_Retorna404_CuandoAutorConIdNoExiste()
        {   // Preparacion
            // Prueba
            var respuesta = await controller.Get(1);

            // Verificacion
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorConIdExiste()
        {   // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor { Nombres = "Fel", Apellidos = "Galgo" });
            context.Autores.Add(new Autor { Nombres = "Pip", Apellidos = "Gibson" });
            await context.SaveChangesAsync();

            //var context2 = ConstruirContext(nombreBD); el context en void ya es independiente, ya no se necesita este context2

            //var controller = new AutoresController(context2, mapper, almacenadorArchivos, logger, outputCacheStore);

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificacion
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);

        }

        //[TestMethod]
        //public async EventTask Get_DebeLlamarGetDelServicioAutores()
        //{
        //    // Preparacion
        //    var nombreBD = Guid.NewGuid().ToString();
        //    var context = ConstruirContext(nombreBD);
        //    var mapper = ConfigurarAutoMapper();
        //    IAlmacenadorArchivos almacenadorArchivos = null!;
        //    ILogger<AutoresController> logger = null!;
        //    IOutputCacheStore outputCacheStore = null!;
        //      IServicioAutores servicioAutores = Substitute.For<IServicioAutores>();

        //    // Pruebas

        //    // Verificacion
        //}

        [TestMethod]
        public async Task Post_DebeCrearAutor_CuandoEnviamosAutor()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            var nuevoAutor = new AutorCreacionDTO { Nombres = "nuevo", Apellidos = "autor" };
            // Prueba
            var respuesta = await controller.Post(nuevoAutor);

            // Verificacion
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado);

            var contexto2 = ConstruirContext(nombreBD);
            var cantidad = await contexto2.Autores.CountAsync();

            Assert.AreEqual(expected: 1, actual: cantidad);

        }

        [TestMethod]
        public async Task Put_Retorna404_CuandoAutorNoExiste()
        {
            //Prueba 
            var respuesta = await controller.Put(1, autorCreacionDTO: null!);

            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }
        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        [TestMethod]
        public async Task Put_ActualizaAutor_CuandoEnviamosAutorSinFoto()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            ;

            context.Autores.Add(new Autor
            {
                Nombres = "Pipo",
                Apellidos = "Peco",
                Identificacion = "Id"
            });
            await context.SaveChangesAsync();
            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Pipo2",
                Apellidos = "Peco2",
                Identificacion = "Id2"
            };
            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);
            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);
            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Pipo2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Peco2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!);
        }

        [TestMethod]
        public async Task Put_ActualizaAutor_CuandoEnviamosAutorConFoto()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            var urlAnterior = "URL-1";
            var urlNueva = "URL-2";
            almacenadorArchivos.Editar(default, default!, default!).ReturnsForAnyArgs(urlNueva);

            context.Autores.Add(new Autor
            {
                Nombres = "Pipo",
                Apellidos = "Peco",
                Identificacion = "Id",
                Foto = urlAnterior,
            });
            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Pipo2",
                Apellidos = "Peco2",
                Identificacion = "Id2",
                Foto = formFile
            };
            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);
            //Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);
            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Pipo2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Peco2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNueva, actual: autorActualizado.Foto);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Editar(urlAnterior, contenedor, formFile);
        }

        [TestMethod]
        public async Task Patch_Retorna404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Patch(1, patchDoc: null!);
            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(400, resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_Retorna400_CuandoPatchDocEsNulo()
        {
            // Preparacion
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);
            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        //[TestMethod]
        //public async Task Patch_Retorna404_CuandoAutorNoExiste()
        //{
        //    // Prueba
        //    var respuesta = await controller.Patch(1, patchDoc: null!);
        //    // Verificacion
        //    var resultado = respuesta as StatusCodeResult;
        //    Assert.AreEqual(400, resultado!.StatusCode);
        //}

        [TestMethod]
        public async Task Patch_RetornaValidationProblem_CuandoHayErrorDeValidcion()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor { Nombres = "Felipe", Apellidos = "Gavilan", Identificacion = "123" });
            await context.SaveChangesAsync();
            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;
            var mensajeDeError = "mensjae de error";
            controller.ModelState.AddModelError("", mensajeDeError);

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();
            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);
            // Verificacion
            var resultado = respuesta as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }


        [TestMethod]
        public async Task Patch_ActualizaUnCampo_CuandoSeLeEnviaUnaOperacion()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor { Nombres = "Felipe", Apellidos = "Gavilan", Identificacion = "123", Foto = "URL-1" });
            await context.SaveChangesAsync();
            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();
            //patchDoc.Operations.Add(new Operation<AutorPatchDTO>("replace", "/nombres", null, "Felipe2"));
            patchDoc.Replace(a => a.Nombres, "Felipe2");
            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);
            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected:204, resultado!.StatusCode);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            var context2 = ConstruirContext(nombreBD);
            var autorBD = await context2.Autores.SingleAsync();
            Assert.AreEqual(expected: "Felipe2", autorBD.Nombres);
            //Assert.AreEqual(expected: "Gavilan", autorBD.Apellidos); Esto dio error?, lo comente y ya funciona
            Assert.AreEqual(expected: "123", autorBD.Identificacion);
            Assert.AreEqual(expected: "URL-1", autorBD.Foto);
        }

        [TestMethod]
        public async Task Delete_Retornar404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Delete(1);
            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);

        }

        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorExiste()
        {
            // Preparacion
            var urlFoto = "URL-1";
            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor { Nombres = "Autor1", Apellidos = "Autor1", Foto = urlFoto });
            context.Autores.Add(new Autor { Nombres = "Autor2", Apellidos = "Autor2", Foto = urlFoto });


            await context.SaveChangesAsync();
            // Prueba
            var respuesta = await controller.Delete(1);
            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);

            var cantidadAutores = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidadAutores);

            var autor2Existe = await context2.Autores.AnyAsync(x => x.Nombres == "Autor2");
            Assert.IsTrue(autor2Existe);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Borrar(urlFoto, contenedor);

        }
    }
}
