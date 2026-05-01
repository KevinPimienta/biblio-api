using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Dynamic.Core;

namespace BibliotecaAPI.Controller.V1
{
    [ApiController]
    [Route("api/v1/autores")]
    [Authorize(Policy = "esadmin")]
    //[FiltroAgregarCabeceras("controlador", "autor")]
    public class AutoresController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly ILogger<AutoresController> logger;
        private readonly IOutputCacheStore outputCacheStore;
        private const string contenedor = "autores";
        private const string cache = "autores-obtener";



        //private readonly ILogger<AutoresController> logger;
        //public AutoresController(ApplicationDbContext context, ILogger<AutoresController> logger) //<-- Intestigar inyeccion de dependeicas como estas
        //{
        //    this.context = context;
        //    this.logger = logger;
        //}

        public AutoresController(ApplicationDbContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos, ILogger<AutoresController> logger,
            IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet(Name = "ObtenerAutoresV1")]// api/autores
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
       // [ServiceFilter<MiFiltroDeAccion>()]
       // [FiltroAgregarCabeceras("accion", "obtener-autores")]
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            //logger.LogInformation("Yes");
            var queryable = context.Autores.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var autores = await queryable
                .OrderBy(x => x.Nombres)
                .Paginar(paginacionDTO).ToListAsync();
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                
            //    autores.Select(autor => new AutorDTO
            //{
            //      Id = autor.Id,
            //      NombreCompleto = $"{autor.Nombres} {autor.Apellidos}"
            //});
            return autoresDTO;
        }

        [HttpGet("{id:int}", Name="ObtenerAutorV1")]
        [AllowAnonymous]
        [EndpointSummary("Obtiene autor por Id")]
        [EndpointDescription("Obtiene un autor pro su Id. Incluyes sus libros.")]
        [ProducesResponseType<AutorConLibrosDTOs>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<AutorConLibrosDTOs>> Get([Description("El ID del autor")]int id)
        {
            var autor = await context.Autores
                .Include(x => x.Libros)
                .ThenInclude(x => x.Libro)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (autor == null) { 
             return NotFound();
            }
            var autorDTO = mapper.Map<AutorConLibrosDTOs>(autor);
           // GenerarEnlaces(autorDTO);
            return autorDTO;
        }

        private void GenerarEnlaces(AutorDTO autorDTO)
        {
            autorDTO.Enlaces.Add(
                new DatosHATEOSDTO(
                    Enlace: Url.Link("ObtenerAutorV1", new { id = autorDTO.Id })!,
                    Descripcion: "self",
                    Metodo: "GET"));

            autorDTO.Enlaces.Add(
                new DatosHATEOSDTO(
                    Enlace: Url.Link("ActualizarAutorV1", new { id = autorDTO.Id })!,
                    Descripcion: "autor-actualizar",
                    Metodo: "PUT"));

            autorDTO.Enlaces.Add(
                new DatosHATEOSDTO(
                    Enlace: Url.Link("PatchAutorV1", new { id = autorDTO.Id })!,
                    Descripcion: "autor-patch",
                    Metodo: "PATCH"));

            autorDTO.Enlaces.Add(
                new DatosHATEOSDTO(
                    Enlace: Url.Link("EliminarAutorV1", new { id = autorDTO.Id })!,
                    Descripcion: "autor-borrar",
                    Metodo: "DELETE"));
        }

        [HttpGet("filtra", Name = "FiltraAutoresV1")]
        [AllowAnonymous]
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
            var queryable = context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
            {
                queryable = queryable.Where(x => x.Nombres.Contains(autorFiltroDTO.Nombres));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
            {
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));
            }

            if (autorFiltroDTO.IncluirLibros)
            {
                queryable = queryable.Include(x => x.Libros).ThenInclude(x => x.Libro);
            }

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                if (autorFiltroDTO.TieneFoto.Value)
                {
                    queryable = queryable.Where(x => x.Foto != null);
                }
                else
                {
                    queryable = queryable.Where(x => x.Foto == null);
                }
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                if(autorFiltroDTO.TieneLibros.Value)
                {
                    queryable = queryable.Where(x => x.Libros.Any());
                }
                else
                {
                    queryable = queryable.Where(x => !x.Libros.Any());
                }
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
            {
                queryable = queryable.Where(x => x.Libros.Any(y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro)));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenAscendente ? "ascending" : "descending";

                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch(Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.Nombres);
                    logger.LogError(ex.Message, ex);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombres);
            }

                var autores = await queryable
                    .Paginar(autorFiltroDTO.PaginacionDTO).ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorConLibrosDTOs>>(autores);
                return Ok(autoresDTO);
            }
            else {
                var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDTO);
            }
               
        }

        [HttpPost(Name = "CrearAutorV1")]
        public async Task<ActionResult> Post(AutorCreacionDTO autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            context.Add(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV1", new { id = autor.Id }, autorDTO);
        }

        [HttpPost("con-foto", Name = "CrearAutorConFotoV1")]
        public async Task<ActionResult> PostConFoto([FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            if(autorCreacionDTO.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Add(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV1", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}", Name = "ActulizarAutorV1")]
        public async Task<ActionResult> Put(int id, [FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existeAutor)
            {
                return NotFound();
            }

            var autor = mapper.Map<Autor>(autorCreacionDTO);
            autor.Id = id;

            if (autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await context
                                   .Autores.Where(x => x.Id == id)
                                   .Select(x => x.Foto).FirstAsync();

                var url = await almacenadorArchivos.Editar(fotoActual, contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }


            context.Update(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            return NoContent();
        }

        [HttpPatch("{id:int}", Name ="PatchAutorV1" )]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var autorDB = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autorDB is null)
            {
                return NotFound();
            }

            var autorPatchDTO = mapper.Map<AutorPatchDTO>(autorDB);

            patchDoc.ApplyTo(autorPatchDTO, ModelState);

            var esValido = TryValidateModel(autorPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(autorPatchDTO, autorDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();

        }


        [HttpDelete("{id:int}", Name = "BorrarAutorV1")]
        public async Task<ActionResult> Delete(int id)
        {
            //var registrosBorrados = await context.Autores.Where(x => x.Id == id).ExecuteDeleteAsync();
            // if (registrosBorrados ==0)
            //{
            //    return NotFound();
            //}

            var autor = await context.Autores.FirstOrDefaultAsync(x =>x.Id == id);
            if (autor is null)
            {
                return NotFound();
            }

            context.Remove(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);


             return NoContent();
        }
    }
}
