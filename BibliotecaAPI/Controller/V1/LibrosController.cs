using AutoMapper;
using AutoMapper.Configuration.Annotations;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controller.V1
{
    [ApiController]
    [Route("api/v1/libros")]
    [Authorize(Policy = "esadmin")]
    public class LibrosController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cache = "libros-obtener";

        // private readonly ITimeLimitedDataProtector protectorLimitadiPorTiempo;

        public LibrosController(ApplicationDbContext context, IMapper mapper, IOutputCacheStore outputCacheStore) //IDataProtectionProvider protectionProvider
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
            //protectorLimitadiPorTiempo = protectionProvider.CreateProtector("LibrosController").ToTimeLimitedDataProtector();
        }

        //[HttpGet("listado/obtener-token")]
        //public ActionResult ObtenerTokenListado()
        //{ 
        //    var textoPlano = Guid.NewGuid().ToString();
        //    var token = protectorLimitadiPorTiempo.Protect(textoPlano, lifetime: TimeSpan.FromSeconds(30));
        //    var url = Url.RouteUrl("ObtenerListadoLibrosUsandoToken", new { token }, "https");
        //    return Ok(new { url });
        //}

        //[HttpGet("listado/{token}", Name = "ObtenerListadoLibrosUsandoToken")]
        //[AllowAnonymous]
        //public async Task<ActionResult> ObtenerListadoUsandoToken(string token)
        //{
        //    try
        //    {
        //        protectorLimitadiPorTiempo.Unprotect(token);
        //    }
        //    catch
        //    {
        //        ModelState.AddModelError(nameof(token), "El token ha expirado");
        //        return ValidationProblem();
        //    }


        //    var libros = await context.Libros.ToListAsync();
        //    var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);

        //    return Ok(librosDTO);
        //}

        [HttpGet(Name = "ObtenerLibrosLibroV1")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<LibroDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            var queryable = context.Libros.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var libros = await queryable.OrderBy(x => x.Titulo).Paginar(paginacionDTO).ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);

            return librosDTO;
        }

        [HttpGet("{id:int}", Name = "ObtenerLibroV1")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<LibroConAutoresDTO>> Get(int id)
        {
            var libro = await context.Libros
                .Include(x => x.Autores)
                    .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (libro is null)
            {
                return NotFound();
            }

            var libroDTO = mapper.Map<LibroConAutoresDTO>(libro);

            return libroDTO;
        }

        [HttpPost(Name = "CrearLibroV1")]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {

            //if(libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0) <==== TODO esto esta en FiltroValidacionLibro.cs
            //{
            //    ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
            //        "No se puede crear un libro sin autores");
            //    return ValidationProblem();
            //}

            //var autoresIdsExisten = await context.Autores.Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
            //                        .Select(x => x.Id).ToListAsync();   

            //if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            //{
            //    var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
            //    var autoresNoExistenString = string.Join(", ", autoresNoExisten);
            //    var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
            //    ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),mensajeDeError);
            //    return ValidationProblem();
            //}

            var libro = mapper.Map<Libro>(libroCreacionDTO); 
            AsignarOrdenAutores(libro);
            //var existeAutor = await context.Autores.AnyAsync(x => x.Id == libro.AutorId); Todo esto es verificacion de uno a uno
            //if (!existeAutor)
            //{
            //    ModelState.AddModelError(nameof(libro.AutorId), $"El autor de id {libro.AutorId} no existe");
            //    return ValidationProblem();// Problem details activo
            //    //return BadRequest($"El autor de id {libro.AutorId} no existe"); <-- error sin detalles. Es un simple texto.
            //}
            context.Libros.Add(libro);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            var libroDTO = mapper.Map<LibroDTO>(libro);

            return CreatedAtRoute("ObtenerLibrosV1", new { id = libro.Id }, libroDTO);
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores is not null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i;
                }
            }
        }

        [HttpPut("{id:int}", Name = "ActualizarLibroV1")]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            //if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)  <==== TODO esto esta en FiltroValidacionLibro.cs
            //{
            //    ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
            //        "No se puede crear un libro sin autores");
            //    return ValidationProblem();
            //}

            //var autoresIdsExisten = await context.Autores.Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
            //                        .Select(x => x.Id).ToListAsync();

            //if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            //{
            //    var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
            //    var autoresNoExistenString = string.Join(", ", autoresNoExisten);
            //    var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
            //    ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensajeDeError);
            //    return ValidationProblem();
            //}

            var libroDB = await context.Libros
                            .Include(x => x.Autores)
                            .FirstOrDefaultAsync(x => x.Id == id);

            if (libroDB is null)
            {
                return NotFound();
            }

            libroDB = mapper.Map(libroCreacionDTO, libroDB);
            AsignarOrdenAutores(libroDB);

            //var libro = mapper.Map<Libro>(libroCreacionDTO);
            //libro.Id = id;

            //var existeAutor = await context.Autores.AnyAsync(x => x.Id == libro.AutorId); update de uno a uno
            //if (!existeAutor)
            //{
            //    return BadRequest($"El autor de id {libro.AutorId} no existe");
            //}
            //context.Update(libro);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            return NoContent();
        }

        [HttpDelete("{id:int}", Name = "EliminarLibro")]
        public async Task<ActionResult> Delete(int id)
        {
             var registrosBorrados = await context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();
            if (registrosBorrados == 0)
            {
                return NotFound();
            }
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}
